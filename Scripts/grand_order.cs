using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Advisor.Scripts;

internal sealed class GrandOrderSnapshot
{
    public string Scene { get; set; } = "";
    public string Option { get; set; } = "";
    public string Description { get; set; } = "";
    public long TimestampMs { get; set; }
}

internal sealed class GrandOrderTeammateObservation
{
    /// <summary>Teammate identity key (typically Player.NetId as string).</summary>
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<GrandOrderSnapshot> Snapshots { get; set; } = new();
}

internal sealed class GrandOrderWindowState
{
    public bool DetailsHidden { get; set; }
    public float DetailsX { get; set; } = 16;
    public float DetailsY { get; set; } = 96;
}

internal sealed class GrandOrderRegistryData
{
    public string SchemaVersion { get; set; } = "1";
    public GrandOrderWindowState Window { get; set; } = new();
    public List<GrandOrderTeammateObservation> Observations { get; set; } = new();
}

internal static class GrandOrderRegistry
{
    private const string SchemaVersion = "2";
    private const string FileName = "grand_order_registry.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private static string GetRegistryPath()
    {
        // Use Godot's sandboxed user data dir so it works in game.
        string dir = OS.GetUserDataDir();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.GetTempPath();

        dir = Path.Combine(dir, "mods", "sts-2-advisor");
        return Path.Combine(dir, FileName);
    }

    internal static GrandOrderRegistryData LoadOrCreate()
    {
        string path = GetRegistryPath();
        try
        {
            if (!File.Exists(path))
            {
                var created = CreateDefaultData();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Save(created);
                return created;
            }

            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<GrandOrderRegistryData>(json, JsonOptions);
            if (data == null || !string.Equals(data.SchemaVersion, SchemaVersion, StringComparison.Ordinal))
                return CreateDefaultData();

            data.Window ??= new GrandOrderWindowState();
            data.Observations ??= new List<GrandOrderTeammateObservation>();
            return data;
        }
        catch (Exception e)
        {
            Log.Error($"[grand_order] Registry load failed, fallback to default: {e}");
            return CreateDefaultData();
        }
    }

    internal static void Save(GrandOrderRegistryData data)
    {
        string path = GetRegistryPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Log.Error($"[grand_order] Registry save failed: {e}");
        }
    }

    internal static GrandOrderTeammateObservation EnsureObservation(
        GrandOrderRegistryData data,
        string teammateKey,
        string displayName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var existing = data.Observations.FirstOrDefault(o => string.Equals(o.Key, teammateKey, StringComparison.Ordinal));
        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal))
                existing.DisplayName = displayName;
            existing.Snapshots ??= new List<GrandOrderSnapshot>();
            return existing;
        }

        var snapshots = new List<GrandOrderSnapshot>();

        var created = new GrandOrderTeammateObservation
        {
            Key = teammateKey,
            DisplayName = displayName,
            Snapshots = snapshots,
        };

        data.Observations.Add(created);
        return created;
    }

    private static GrandOrderRegistryData CreateDefaultData()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new GrandOrderRegistryData
        {
            SchemaVersion = SchemaVersion,
            Window = new GrandOrderWindowState
            {
                DetailsHidden = false,
                DetailsX = 16,
                DetailsY = 96,
            },
            Observations = new List<GrandOrderTeammateObservation>
            {
                new()
                {
                    Key = "default",
                    DisplayName = STS2AdvisorI18n.Pick("Teammate", "队友"),
                    Snapshots = new List<GrandOrderSnapshot>()
                }
            }
        };
    }

    // Keep legacy stub for older registry files; now unused.
    private static List<GrandOrderSnapshot> CreateDefaultSnapshots() => new();
}

public partial class GrandOrderPanel : CanvasLayer
{
    public static GrandOrderPanel? Instance { get; private set; }

    private const float HoverOffsetX = 14;
    private const float HoverOffsetY = 14;
    private const float DetailsHeaderHeight = 44;

    private PanelContainer? _detailsRoot;
    private Label? _detailsTitle;
    private VBoxContainer? _snapshotList;
    private ScrollContainer? _snapshotScroll;

    private PanelContainer? _hoverRoot;
    private Label? _hoverName;
    private Label? _hoverSummary;
    private Label? _hoverTime;

    private HBoxContainer? _teammateBar;
    private Label? _selectedLabel;

    private readonly List<(Control control, string teammateKey)> _barItems = new();

    private bool _draggingDetails;
    private Vector2 _detailsDragOffset;
    private float _scale = 1.0f;

    private GrandOrderRegistryData _registry = new();
    private string? _selectedKey;
    private string? _hoverKey;

    // ── 字符串 i18n helper ─────────────────────────────────────────
    private static string T(string en, string zh) => STS2AdvisorI18n.Pick(en, zh);

    public override void _Ready()
    {
        Instance = this;
        Layer = 98;
        _registry = GrandOrderRegistry.LoadOrCreate();

        BuildUi();

        SetDetailsVisible(!_registry.Window.DetailsHidden);
        if (_detailsRoot != null)
        {
            _detailsRoot.Position = new Vector2(_registry.Window.DetailsX, _registry.Window.DetailsY);
        }

        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;

        // Ensure net binding exists even if lifecycle patches run earlier.
        try
        {
            var net = RunManager.Instance.IsInProgress ? RunManager.Instance.NetService : null;
            GrandOrderNetSync.BindToNetService(net);
        }
        catch (Exception e)
        {
            Log.Error($"[grand_order] net bind failed: {e}");
        }

        UpdateTeammateBar();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        try
        {
            CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
        }
        catch
        {
            // Ignore if CombatManager already freed.
        }
    }

    private void OnCombatStateChanged(CombatState _)
    {
        UpdateTeammateBar();
    }

    internal void ApplyLocalSnapshotWithoutNet(GrandOrderNetSnapshot snapshot)
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        var local = state?.Players?.FirstOrDefault();
        if (local == null)
            return;

        string key = SafePlayerKey(local);
        AppendSnapshot(key, snapshot.Scene, snapshot.Option, snapshot.Description);
    }

    internal void ApplyNetSnapshot(ulong senderId, GrandOrderNetSnapshot? snapshot)
    {
        string key = senderId.ToString();

        if (snapshot == null)
        {
            ClearSnapshotsFor(key);
            return;
        }

        AppendSnapshot(key, snapshot.Scene, snapshot.Option, snapshot.Description);
    }

    internal void ClearLocalSnapshots()
    {
        var state = CombatManager.Instance.DebugOnlyGetState();
        var local = state?.Players?.FirstOrDefault();
        if (local == null) return;
        ClearSnapshotsFor(SafePlayerKey(local));
    }

    internal void ClearAllSnapshots()
    {
        foreach (var obs in _registry.Observations)
            obs.Snapshots.Clear();

        GrandOrderRegistry.Save(_registry);
        HideHover();
        if (_selectedKey != null)
            RefreshDetails(_selectedKey);
    }

    private void ClearSnapshotsFor(string teammateKey)
    {
        var obs = _registry.Observations.FirstOrDefault(o => o.Key == teammateKey);
        if (obs == null) return;
        obs.Snapshots.Clear();
        GrandOrderRegistry.Save(_registry);

        if (_selectedKey == teammateKey)
            RefreshDetails(teammateKey);
    }

    private void AppendSnapshot(string teammateKey, string scene, string option, string description)
    {
        if (string.IsNullOrWhiteSpace(scene) && string.IsNullOrWhiteSpace(option) && string.IsNullOrWhiteSpace(description))
            return;

        // Find display name if we can; otherwise keep existing.
        string? displayName = _registry.Observations.FirstOrDefault(o => o.Key == teammateKey)?.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = STS2AdvisorI18n.Pick("Teammate", "队友");

        var obs = GrandOrderRegistry.EnsureObservation(_registry, teammateKey, displayName);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string signature = $"{scene}\n{option}\n{description}".Trim();

        var last = obs.Snapshots.Count > 0 ? obs.Snapshots[^1] : null;
        string lastSig = last == null ? "" : $"{last.Scene}\n{last.Option}\n{last.Description}".Trim();

        if (last != null && string.Equals(signature, lastSig, StringComparison.Ordinal))
        {
            // Same content; just update timestamp if it's newer.
            if (now > last.TimestampMs)
                last.TimestampMs = now;
            return;
        }

        obs.Snapshots.Add(new GrandOrderSnapshot
        {
            Scene = scene,
            Option = option,
            Description = description,
            TimestampMs = now,
        });

        // Bound history size per teammate.
        const int MaxSnapshotsPerTeammate = 25;
        if (obs.Snapshots.Count > MaxSnapshotsPerTeammate)
            obs.Snapshots.RemoveRange(0, obs.Snapshots.Count - MaxSnapshotsPerTeammate);

        GrandOrderRegistry.Save(_registry);

        if (_selectedKey == teammateKey)
            RefreshDetails(teammateKey);
    }

    private void BuildUi()
    {
        // Bar: compact clickable teammates list.
        var barRoot = new PanelContainer();
        barRoot.Name = "grand_order_bar";
        barRoot.Position = new Vector2(16, 16);
        barRoot.CustomMinimumSize = new Vector2(0, 34);
        barRoot.AddThemeStyleboxOverride("panel", MakeBarStyle());
        AddChild(barRoot);
        _teammateBar = new HBoxContainer();
        _teammateBar.AddThemeConstantOverride("separation", 6);
        _teammateBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        barRoot.AddChild(_teammateBar);

        // Details panel (draggable).
        _detailsRoot = new PanelContainer();
        _detailsRoot.Name = "grand_order_details";
        _detailsRoot.CustomMinimumSize = new Vector2((int)(320 * _scale), 0);
        _detailsRoot.AddThemeStyleboxOverride("panel", MakeDetailsStyle());
        _detailsRoot.Position = new Vector2(_registry.Window.DetailsX, _registry.Window.DetailsY);
        AddChild(_detailsRoot);

        var detailsOuter = new VBoxContainer();
        detailsOuter.AddThemeConstantOverride("separation", 0);
        _detailsRoot.AddChild(detailsOuter);

        // Header
        var detailsHeader = new PanelContainer();
        detailsHeader.AddThemeStyleboxOverride("panel", MakeHeaderStyle());
        detailsOuter.AddChild(detailsHeader);
        detailsHeader.CustomMinimumSize = new Vector2(0, DetailsHeaderHeight);

        var headerBox = new HBoxContainer();
        headerBox.AddThemeConstantOverride("separation", 8);
        detailsHeader.AddChild(headerBox);

        var dot = new Panel();
        dot.CustomMinimumSize = new Vector2(9, 9);
        dot.AddThemeStyleboxOverride("panel", MakeDotStyle());
        dot.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        headerBox.AddChild(dot);

        _detailsTitle = new Label();
        _detailsTitle.Text = T("Teammate Observer", "队友观察");
        _detailsTitle.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
        _detailsTitle.AddThemeFontSizeOverride("font_size", 13);
        headerBox.AddChild(_detailsTitle);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerBox.AddChild(spacer);

        // Hide
        var btnHide = new Button();
        btnHide.Text = "×";
        btnHide.Flat = true;
        btnHide.CustomMinimumSize = new Vector2(28, 24);
        btnHide.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.85f, 1f));
        btnHide.AddThemeFontSizeOverride("font_size", 16);
        btnHide.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        btnHide.Pressed += () =>
        {
            SetDetailsVisible(false);
            _registry.Window.DetailsHidden = true;
            PersistWindowState();
            HideHover();
        };
        headerBox.AddChild(btnHide);

        // Body
        var bodyPad = new MarginContainer();
        bodyPad.AddThemeConstantOverride("margin_left", 12);
        bodyPad.AddThemeConstantOverride("margin_right", 12);
        bodyPad.AddThemeConstantOverride("margin_top", 10);
        bodyPad.AddThemeConstantOverride("margin_bottom", 12);
        detailsOuter.AddChild(bodyPad);

        var bodyOuter = new VBoxContainer();
        bodyPad.AddChild(bodyOuter);

        _selectedLabel = new Label();
        _selectedLabel.Text = "";
        _selectedLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f, 1f));
        _selectedLabel.AddThemeFontSizeOverride("font_size", 11);
        bodyOuter.AddChild(_selectedLabel);

        _snapshotScroll = new ScrollContainer();
        _snapshotScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _snapshotScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _snapshotScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        bodyOuter.AddChild(_snapshotScroll);

        _snapshotList = new VBoxContainer();
        _snapshotList.AddThemeConstantOverride("separation", 6);
        _snapshotScroll.AddChild(_snapshotList);
    }

    private void SetDetailsVisible(bool visible)
    {
        if (_detailsRoot == null) return;
        _detailsRoot.Visible = visible;
    }

    private void UpdateTeammateBar()
    {
        if (_teammateBar == null) return;
        _teammateBar.QueueFreeChildren();
        _barItems.Clear();
        HideHover();

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state?.Players == null || state.Players.Count == 0)
        {
            _teammateBar.Visible = false;
            SetDetailsVisible(false);
            return;
        }

        _teammateBar.Visible = true;

        // Build items from current combat players (works even before we have snapshot sync).
        foreach (var p in state.Players)
        {
            string key = SafePlayerKey(p);
            string name = LocText.Of(p.Character);

            var obs = GrandOrderRegistry.EnsureObservation(_registry, key, name);
            // Persist only when registry changed (lazy: we'll just save after bar rebuild).
            // Real sync/dedup comes later.

            var item = new Button();
            item.Flat = true;
            item.Text = ShortName(obs.DisplayName);
            item.CustomMinimumSize = new Vector2(80, 26);
            item.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.95f, 1f));
            item.AddThemeFontSizeOverride("font_size", 12);
            item.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

            _teammateBar.AddChild(item);
            _barItems.Add((item, key));
        }

        // Ensure we keep newly created observations.
        GrandOrderRegistry.Save(_registry);

        // If current selection is invalid, pick first.
        var firstKey = _barItems.FirstOrDefault().teammateKey;
        if (string.IsNullOrWhiteSpace(_selectedKey) && !string.IsNullOrWhiteSpace(firstKey))
        {
            _selectedKey = firstKey;
            RefreshDetails(_selectedKey);
        }
    }

    private void RefreshDetails(string teammateKey)
    {
        if (_detailsRoot == null || _snapshotList == null) return;

        var selectedObs = _registry.Observations.FirstOrDefault(o => o.Key == teammateKey)
                           ?? _registry.Observations.FirstOrDefault();

        if (_selectedLabel != null)
        {
            string disp = selectedObs?.DisplayName ?? "";
            _selectedLabel.Text = string.IsNullOrWhiteSpace(disp)
                ? ""
                : T("Viewing:", "查看：") + " " + disp;
        }

        foreach (var child in _snapshotList.GetChildren())
            child.QueueFree();

        var snapshots = selectedObs?.Snapshots ?? new List<GrandOrderSnapshot>();
        if (snapshots.Count == 0)
        {
            var empty = new Label();
            empty.Text = T("No snapshots yet.", "暂无快照。");
            empty.AddThemeColorOverride("font_color", new Color(0.62f, 0.66f, 0.75f, 1f));
            empty.AddThemeFontSizeOverride("font_size", 12);
            empty.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _snapshotList.AddChild(empty);
            return;
        }

        foreach (var s in snapshots.OrderByDescending(x => x.TimestampMs))
        {
            _snapshotList.AddChild(BuildSnapshotCard(s));
        }
    }

    private Control BuildSnapshotCard(GrandOrderSnapshot s)
    {
        var card = new PanelContainer();
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.10f, 0.11f, 0.15f, 0.55f);
        style.SetCornerRadiusAll(9);
        style.SetBorderWidthAll(1);
        style.BorderColor = new Color(1f, 1f, 1f, 0.08f);
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        card.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        card.AddChild(vbox);

        var line1 = new Label();
        line1.Text = $"{T("Scene", "场景")}: {s.Scene}";
        line1.AddThemeColorOverride("font_color", new Color(0.90f, 0.92f, 0.95f, 1f));
        line1.AddThemeFontSizeOverride("font_size", 12);
        line1.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(line1);

        var line2 = new Label();
        line2.Text = $"{T("Option", "选项")}: {s.Option}";
        line2.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f, 1f));
        line2.AddThemeFontSizeOverride("font_size", 12);
        line2.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(line2);

        var desc = new Label();
        desc.Text = $"{T("Description", "描述")}: {s.Description}";
        desc.AddThemeColorOverride("font_color", new Color(0.62f, 0.66f, 0.75f, 1f));
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(desc);

        var time = new Label();
        time.Text = $"{T("Time", "时间")}: {FormatTimestamp(s.TimestampMs)}";
        time.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f, 1f));
        time.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(time);

        return card;
    }

    private static string FormatTimestamp(long timestampMs)
    {
        if (timestampMs <= 0) return "";
        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).ToLocalTime().DateTime;
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return timestampMs.ToString();
        }
    }

    private static string SafePlayerKey(Player player)
    {
        try
        {
            return player.NetId.ToString();
        }
        catch
        {
            // Fallback for unknown player model shape.
            return Guid.NewGuid().ToString("N");
        }
    }

    private static string ShortName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        if (name.Length <= 6) return name;
        return name[..6] + "...";
    }

    public override void _Input(InputEvent evt)
    {
        if (_detailsRoot == null) return;

        // Hotkey: show/hide details panel.
        if (evt is InputEventKey key &&
            key.Pressed &&
            key.Keycode == HotkeyConfig.GetKey(HotkeyAction.GrandOrderToggleDetails))
        {
            bool nextVisible = !_detailsRoot.Visible;
            SetDetailsVisible(nextVisible);
            PersistWindowState();
            HideHover();

            if (nextVisible && !string.IsNullOrWhiteSpace(_selectedKey))
                RefreshDetails(_selectedKey!);

            return;
        }

        // Details dragging.
        if (_detailsRoot.Visible && evt is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                var local = _detailsRoot.GetLocalMousePosition();
                bool inHeader = local.Y >= 0 && local.Y <= DetailsHeaderHeight
                                 && local.X >= 0 && local.X <= _detailsRoot.Size.X;
                if (inHeader)
                {
                    if (!PanelDragState.TryStart(nameof(GrandOrderPanel))) return;
                    _draggingDetails = true;
                    _detailsDragOffset = _detailsRoot.Position - GetViewport().GetMousePosition();
                }
                else
                {
                    _draggingDetails = false;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
            {
                if (_draggingDetails)
                {
                    _draggingDetails = false;
                    PanelDragState.End(nameof(GrandOrderPanel));
                    PersistWindowState();
                }
            }
        }

        if (_detailsRoot.Visible && _draggingDetails && evt is InputEventMouseMotion)
        {
            Vector2 desired = GetViewport().GetMousePosition() + _detailsDragOffset;
            _detailsRoot.Position = ClampToViewport(desired, _detailsRoot);
        }

        // Hover + open details from teammate bar.
        if (_teammateBar == null || !_teammateBar.Visible) return;

        Vector2 mouse = GetViewport().GetMousePosition();
        if (evt is InputEventMouseMotion)
        {
            var hit = HitBarItem(mouse);
            if (hit.HasValue)
            {
                var (itemCtrl, hitKey) = hit.Value;
                if (_selectedKey == null || !_selectedKey.Equals(hitKey, StringComparison.Ordinal))
                {
                    // For hover only; details change on click.
                }
                ShowHoverFor(hitKey, mouse);
            }
            else
            {
                HideHover();
            }
        }

        if (evt is InputEventMouseButton mb2 && mb2.ButtonIndex == MouseButton.Left && mb2.Pressed)
        {
            var hit = HitBarItem(mouse);
            if (hit.HasValue)
            {
                var (_, hitKey) = hit.Value;
                OpenDetailsFor(hitKey, mouse);
            }
            else if (_hoverRoot != null && _hoverRoot.Visible && _hoverRoot.GetGlobalRect().HasPoint(mouse))
            {
                // Allow click-through from hover card.
                if (_hoverKey != null)
                    OpenDetailsFor(_hoverKey, mouse);
            }
        }
    }

    private (Control control, string teammateKey)? HitBarItem(Vector2 globalMouse)
    {
        foreach (var (control, key) in _barItems)
        {
            if (control == null || !GodotObjectIsAlive(control))
                continue;

            if (control.GetGlobalRect().HasPoint(globalMouse))
                return (control, key);
        }
        return null;
    }

    private static bool GodotObjectIsAlive(GodotObject obj) =>
        obj != null && GodotObject.IsInstanceValid(obj);

    private void OpenDetailsFor(string teammateKey, Vector2 anchorGlobalPos)
    {
        _selectedKey = teammateKey;
        RefreshDetails(teammateKey);
        SetDetailsVisible(true);
        _registry.Window.DetailsHidden = false;

        AutoLocateDetails(anchorGlobalPos);
        PersistWindowState();
        HideHover();
    }

    private void AutoLocateDetails(Vector2 anchorGlobalPos)
    {
        if (_detailsRoot == null) return;

        Vector2 desired = anchorGlobalPos + new Vector2(8, DetailsHeaderHeight + 10);
        _detailsRoot.Position = ClampToViewport(desired, _detailsRoot);
        _registry.Window.DetailsX = _detailsRoot.Position.X;
        _registry.Window.DetailsY = _detailsRoot.Position.Y;
    }

    private void PersistWindowState()
    {
        if (_detailsRoot == null) return;
        _registry.Window.DetailsHidden = !_detailsRoot.Visible;
        _registry.Window.DetailsX = _detailsRoot.Position.X;
        _registry.Window.DetailsY = _detailsRoot.Position.Y;
        GrandOrderRegistry.Save(_registry);
    }

    private void EnsureHoverUi()
    {
        if (_hoverRoot != null) return;

        _hoverRoot = new PanelContainer();
        _hoverRoot.Name = "grand_order_hover";
        _hoverRoot.Visible = false;
        _hoverRoot.CustomMinimumSize = new Vector2(0, 0);
        _hoverRoot.AddThemeStyleboxOverride("panel", MakeHoverStyle());
        AddChild(_hoverRoot);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        vbox.AddThemeConstantOverride("margin_top", 4);
        vbox.AddThemeConstantOverride("margin_bottom", 4);
        _hoverRoot.AddChild(vbox);

        _hoverName = new Label();
        _hoverName.AddThemeColorOverride("font_color", new Color(0.95f, 0.97f, 1f, 1f));
        _hoverName.AddThemeFontSizeOverride("font_size", 12);
        _hoverName.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_hoverName);

        _hoverSummary = new Label();
        _hoverSummary.AddThemeColorOverride("font_color", new Color(0.80f, 0.82f, 0.88f, 1f));
        _hoverSummary.AddThemeFontSizeOverride("font_size", 12);
        _hoverSummary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_hoverSummary);

        _hoverTime = new Label();
        _hoverTime.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f, 1f));
        _hoverTime.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_hoverTime);
    }

    private void ShowHoverFor(string teammateKey, Vector2 mouseGlobalPos)
    {
        EnsureHoverUi();
        if (_hoverRoot == null || _hoverName == null || _hoverSummary == null || _hoverTime == null) return;

        _hoverKey = teammateKey;

        var obs = _registry.Observations.FirstOrDefault(o => o.Key == teammateKey) ?? _registry.Observations.FirstOrDefault();
        var latest = obs?.Snapshots?.OrderByDescending(s => s.TimestampMs).FirstOrDefault();
        if (obs == null || latest == null)
        {
            HideHover();
            return;
        }

        _hoverName.Text = obs.DisplayName;
        _hoverSummary.Text = $"{T("Latest", "最新")}: {latest.Scene} - {latest.Option}";
        _hoverTime.Text = $"{FormatTimestamp(latest.TimestampMs)}";

        _hoverRoot.Visible = true;
        Vector2 desired = mouseGlobalPos + new Vector2(HoverOffsetX, HoverOffsetY);
        _hoverRoot.Position = ClampToViewport(desired, _hoverRoot);
    }

    private void HideHover()
    {
        if (_hoverRoot != null) _hoverRoot.Visible = false;
        _hoverKey = null;
    }

    private static Vector2 ClampToViewport(Vector2 desired, Control control)
    {
        var vp = control.GetViewport();
        var rect = vp.GetVisibleRect();

        float w = control.Size.X;
        float h = control.Size.Y;

        // Avoid 0 sizes before first layout.
        if (w <= 0) w = control.CustomMinimumSize.X;
        if (h <= 0) h = control.CustomMinimumSize.Y;

        float x = Mathf.Clamp(desired.X, rect.Position.X, rect.Position.X + rect.Size.X - w);
        float y = Mathf.Clamp(desired.Y, rect.Position.Y, rect.Position.Y + rect.Size.Y - h);
        return new Vector2(x, y);
    }

    private static StyleBoxFlat MakeBarStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.04f, 0.05f, 0.08f, 0.60f);
        s.SetCornerRadiusAll(12);
        s.SetBorderWidthAll(1);
        s.BorderColor = new Color(1f, 1f, 1f, 0.10f);
        return s;
    }

    private static StyleBoxFlat MakeDetailsStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.94f);
        s.SetCornerRadiusAll(14);
        s.SetBorderWidthAll(1);
        s.BorderColor = new Color(1f, 1f, 1f, 0.10f);
        s.ShadowColor = new Color(0, 0, 0, 0.35f);
        s.ShadowSize = 12;
        s.ShadowOffset = new Vector2(0, 4);
        return s;
    }

    private static StyleBoxFlat MakeHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(1, 1, 1, 0.04f);
        s.CornerRadiusTopLeft = 14;
        s.CornerRadiusTopRight = 14;
        s.ContentMarginLeft = 14;
        s.ContentMarginRight = 10;
        s.ContentMarginTop = 12;
        s.ContentMarginBottom = 12;
        return s;
    }

    private static StyleBoxFlat MakeDotStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.40f, 0.85f, 0.60f, 1f);
        s.SetCornerRadiusAll(4);
        return s;
    }

    private static StyleBoxFlat MakeHoverStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        s.SetCornerRadiusAll(10);
        s.SetBorderWidthAll(1);
        s.BorderColor = new Color(1f, 1f, 1f, 0.10f);
        s.ShadowColor = new Color(0, 0, 0, 0.40f);
        s.ShadowSize = 10;
        s.ShadowOffset = new Vector2(0, 4);
        return s;
    }
}

internal static class GrandOrderGodotExtensions
{
    internal static void QueueFreeChildren(this Node parent)
    {
        foreach (var child in parent.GetChildren())
            child.QueueFree();
    }

    internal static T? GetNodeOrNull<T>(this Node root, string name) where T : Node
    {
        if (!root.HasNode(name)) return null;
        return root.GetNode<T>(name);
    }
}

