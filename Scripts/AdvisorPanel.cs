using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public partial class AdvisorPanel : CanvasLayer
{
    private PanelContainer?  _root;
    private Label?           _header;
    private ScrollContainer? _scroll;
    private Tween?           _tween;

    private bool    _dragging;
    private Vector2 _dragOffset;
    private float   _scale    = 1.0f;
    private const float MinScale = 0.5f;
    private const float MaxScale = 2.0f;
    private readonly AdvisorRuntimeCore _runtime = new();

    private sealed class PlayerPanel
    {
        public required PanelContainer Root;
        public required Label Header;
        public required VBoxContainer CardList;
        public required ScrollContainer Scroll;
        public ulong NetId;
    }

    private readonly List<PlayerPanel> _playerPanels = new();

    // ── 字号辅助 ─────────────────────────────────────────────
    private int FS(int b) => Mathf.Max(8, (int)(b * _scale));

    // ── 颜色 ─────────────────────────────────────────────────
    private static readonly Color BgColor      = new(0.10f, 0.06f, 0.05f, 0.93f);
    private static readonly Color BorderColor  = new(0.92f, 0.63f, 0.16f, 0.34f);
    private static readonly Color TitleColor   = new(1.00f, 0.88f, 0.52f, 0.98f);
    private static readonly Color DimColor     = new(0.79f, 0.58f, 0.39f, 1f);

    // 卡牌颜色
    private static readonly Color ColPlayer    = new(0.94f, 0.74f, 0.34f, 1f);
    private static readonly Color ColColorless = new(0.96f, 0.86f, 0.58f, 1f);
    private static readonly Color ColCurse     = new(0.92f, 0.42f, 0.35f, 1f);
    private static readonly Color ColUpgraded  = new(1.00f, 0.88f, 0.52f, 1f);

    // 卡牌背景
    private static readonly Color BgPlayer     = new(0.24f, 0.10f, 0.05f, 0.54f);
    private static readonly Color BgColorless  = new(0.20f, 0.11f, 0.07f, 0.52f);
    private static readonly Color BgCurse      = new(0.28f, 0.08f, 0.10f, 0.58f);

    // ── Godot 生命周期 ───────────────────────────────────────
    public override void _Ready()
    {
        Layer = 99;
        BuildUi();
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_root == null || !_root.Visible || _playerPanels.Count == 0)
            return;
        UpdateDrawPile(force: false, delta: delta);
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
    }

    private void OnCombatStateChanged(CombatState _) => UpdateDrawPile(force: true);

    // ── 缩放（重建 UI 避免模糊）─────────────────────────────
    private void ChangeScale(float delta)
    {
        _scale = Mathf.Clamp(_scale + delta, MinScale, MaxScale);
        var pos = _root?.Position ?? new Vector2(10, 100);
        var visible = _root?.Visible ?? true;
        _root?.QueueFree();
        _root = null;
        _playerPanels.Clear();
        BuildUi();
        if (_root != null)
        {
            _root.Position = pos;
            _root.Visible  = visible;
        }
        _runtime.ResetCache();
        UpdateDrawPile(force: true);
    }

    // ── UI 构建 ──────────────────────────────────────────────
    private void BuildUi()
    {
        _root = new PanelContainer();
        _root.Position = new Vector2(16, 96);
        _root.CustomMinimumSize = new Vector2(0, 0);
        _root.AddThemeStyleboxOverride("panel", MakeRootStyle());
        AddChild(_root);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 0);
        _root.AddChild(outer);

        // ── 标题栏 ──
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", MakeHeaderStyle());
        outer.AddChild(header);

        var headerBox = new HBoxContainer();
        headerBox.AddThemeConstantOverride("separation", 8);
        header.AddChild(headerBox);

        // 绿色指示点
        var dot = new Panel();
        dot.CustomMinimumSize = new Vector2((int)(9 * _scale), (int)(9 * _scale));
        dot.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var dotStyle = new StyleBoxFlat();
        dotStyle.BgColor = new Color(0.92f, 0.63f, 0.16f, 1f);
        dotStyle.SetCornerRadiusAll(4);
        dot.AddThemeStyleboxOverride("panel", dotStyle);
        headerBox.AddChild(dot);

        _header = new Label();
        _header.Text = STS2AdvisorI18n.Pick("Gate of Babylon", "Gate of Babylon");
        _header.AddThemeColorOverride("font_color", TitleColor);
        _header.AddThemeFontSizeOverride("font_size", FS(13));
        _header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerBox.AddChild(_header);

        foreach (var (txt, action) in new (string, System.Action)[]
        {
            ("-", () => ChangeScale(-0.1f)),
            ("+", () => ChangeScale( 0.1f)),
            ("×", () => { if (_root != null) _root.Visible = false; })
        })
        {
            var btn = new Button();
            btn.Text = txt;
            btn.Flat = true;
            btn.CustomMinimumSize = new Vector2(26, 24);
            btn.AddThemeColorOverride("font_color", DimColor);
            btn.AddThemeFontSizeOverride("font_size", FS(txt == "×" ? 16 : 14));
            btn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            btn.Pressed += action;
            headerBox.AddChild(btn);
        }

        // ── 分割线 ──
        var sep = new Panel();
        sep.CustomMinimumSize = new Vector2(0, 1);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = new Color(0.92f, 0.63f, 0.16f, 0.15f);
        sep.AddThemeStyleboxOverride("panel", sepStyle);
        outer.AddChild(sep);

        // ── 범례 (색상 설명) ──
        var legendPad = new MarginContainer();
        legendPad.AddThemeConstantOverride("margin_left",   10);
        legendPad.AddThemeConstantOverride("margin_right",  10);
        legendPad.AddThemeConstantOverride("margin_top",     6);
        legendPad.AddThemeConstantOverride("margin_bottom",  6);
        outer.AddChild(legendPad);

        var legendBox = new HBoxContainer();
        legendBox.AddThemeConstantOverride("separation", 12);
        legendPad.AddChild(legendBox);

        foreach (var (text, color) in new (string, Color)[]
        {
            (STS2AdvisorI18n.Pick("■ Character", "■ 角色"), ColPlayer),
            (STS2AdvisorI18n.Pick("■ Colorless", "■ 无色"), ColColorless),
            (STS2AdvisorI18n.Pick("■ Curse", "■ 诅咒"), ColCurse),
        })
        {
            var lbl = new Label();
            lbl.Text = text;
            lbl.AddThemeColorOverride("font_color", color);
            lbl.AddThemeFontSizeOverride("font_size", FS(10));
            legendBox.AddChild(lbl);
        }

        // ── 分割线 ──
        var sep2 = new Panel();
        sep2.CustomMinimumSize = new Vector2(0, 1);
        sep2.AddThemeStyleboxOverride("panel", sepStyle);
        outer.AddChild(sep2);

        // ── 滚动 + 列表 ──
        _scroll = new ScrollContainer();
        _scroll.CustomMinimumSize    = new Vector2(0, (int)(640 * _scale));
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _scroll.SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill;
        outer.AddChild(_scroll);

        var gridPad = new MarginContainer();
        gridPad.AddThemeConstantOverride("margin_left",   8);
        gridPad.AddThemeConstantOverride("margin_right",  8);
        gridPad.AddThemeConstantOverride("margin_top",    6);
        gridPad.AddThemeConstantOverride("margin_bottom", 8);
        gridPad.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _scroll.AddChild(gridPad);

        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", (int)(10 * _scale));
        grid.AddThemeConstantOverride("v_separation", (int)(10 * _scale));
        grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        gridPad.AddChild(grid);

        // 预创建 4 个玩家框（有几个玩家就显示几个；其余隐藏）
        for (int i = 0; i < 4; i++)
        {
            var pp = BuildPlayerPanel();
            pp.Root.Visible = false;
            _playerPanels.Add(pp);
            grid.AddChild(pp.Root);
        }
    }

    // ── 抽牌堆 + 弃牌堆（完整列表，无张数上限）──────────────────
    public void UpdateDrawPile(bool force = false, double delta = 0)
    {
        if (_header == null || _playerPanels.Count == 0) return;
        if (!_runtime.TryBuildSnapshot(force, delta, out var snapshot)) return;
        int showCount = snapshot.Players.Count;

        _header.Text = STS2AdvisorI18n.Pick(
            $"Gate of Babylon  ({showCount}/{snapshot.TotalPlayers})",
            $"Gate of Babylon（{showCount}/{snapshot.TotalPlayers}）");

        for (int i = 0; i < _playerPanels.Count; i++)
        {
            if (i >= showCount)
            {
                _playerPanels[i].Root.Visible = false;
                continue;
            }

            var player = snapshot.Players[i];
            var pp = _playerPanels[i];
            pp.Root.Visible = true;
            pp.NetId = player.NetId;
            if (!player.Changed)
                continue;

            var drawPile = player.DrawPile;
            var discardPile = player.DiscardPile;

            foreach (var child in pp.CardList.GetChildren())
                child.QueueFree();

            pp.Header.Text = STS2AdvisorI18n.Pick(
                $"{player.Label}  (Draw {drawPile.Count} · Discard {discardPile.Count})",
                $"{player.Label}（抽 {drawPile.Count} · 弃 {discardPile.Count}）");

            pp.CardList.AddChild(BuildSectionTitle(
                STS2AdvisorI18n.Pick(
                    "Draw pile — 1 = next card drawn (top)",
                    "抽牌堆 — 1 为下次抽到的顶牌")));

            for (int c = 0; c < drawPile.Count; c++)
                pp.CardList.AddChild(BuildCardRow(c + 1, drawPile[c]));

            if (player.PredictedMergedShuffle != null)
            {
                pp.CardList.AddChild(BuildSectionSpacer());
                pp.CardList.AddChild(BuildShufflePreviewBanner());
                var predicted = player.PredictedMergedShuffle;
                for (int p = 0; p < predicted.Count; p++)
                    pp.CardList.AddChild(BuildCardRow(p + 1, predicted[p]));
            }

            pp.CardList.AddChild(BuildSectionSpacer());
            pp.CardList.AddChild(BuildSectionTitle(
                STS2AdvisorI18n.Pick(
                    "Discard — 1 = oldest in list; high # = last Bottom-add",
                    "弃牌堆 — 1 为列表最旧；序号大＝最近以 Bottom 入堆")));

            for (int d = 0; d < discardPile.Count; d++)
                pp.CardList.AddChild(BuildCardRow(d + 1, discardPile[d]));
        }
    }

    private PlayerPanel BuildPlayerPanel()
    {
        var root = new PanelContainer();
        root.AddThemeStyleboxOverride("panel", MakeSubPanelStyle());
        root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 0);
        root.AddChild(outer);

        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", MakeSubHeaderStyle());
        outer.AddChild(header);

        var hdrPad = new MarginContainer();
        hdrPad.AddThemeConstantOverride("margin_left", 10);
        hdrPad.AddThemeConstantOverride("margin_right", 10);
        hdrPad.AddThemeConstantOverride("margin_top", 6);
        hdrPad.AddThemeConstantOverride("margin_bottom", 6);
        header.AddChild(hdrPad);

        var hdrLabel = new Label();
        hdrLabel.Text = STS2AdvisorI18n.Pick("Player", "玩家");
        hdrLabel.AddThemeColorOverride("font_color", TitleColor);
        hdrLabel.AddThemeFontSizeOverride("font_size", FS(11));
        hdrLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hdrPad.AddChild(hdrLabel);

        var sep = new Panel();
        sep.CustomMinimumSize = new Vector2(0, 1);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = new Color(0.92f, 0.63f, 0.16f, 0.12f);
        sep.AddThemeStyleboxOverride("panel", sepStyle);
        outer.AddChild(sep);

        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, (int)(420 * _scale));
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        outer.AddChild(scroll);

        var listPad = new MarginContainer();
        listPad.AddThemeConstantOverride("margin_left", 6);
        listPad.AddThemeConstantOverride("margin_right", 6);
        listPad.AddThemeConstantOverride("margin_top", 6);
        listPad.AddThemeConstantOverride("margin_bottom", 6);
        listPad.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(listPad);

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 5);
        list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        listPad.AddChild(list);

        return new PlayerPanel
        {
            Root = root,
            Header = hdrLabel,
            CardList = list,
            Scroll = scroll,
            NetId = 0
        };
    }

    private static StyleBoxFlat MakeSubPanelStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.10f, 0.06f, 0.05f, 0.86f);
        s.SetCornerRadiusAll(12);
        s.BorderColor = new Color(0.92f, 0.63f, 0.16f, 0.24f);
        s.SetBorderWidthAll(1);
        s.ContentMarginLeft = 0;
        s.ContentMarginRight = 0;
        s.ContentMarginTop = 0;
        s.ContentMarginBottom = 0;
        return s;
    }

    private static StyleBoxFlat MakeSubHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.20f, 0.10f, 0.08f, 0.50f);
        s.CornerRadiusTopLeft = 12;
        s.CornerRadiusTopRight = 12;
        s.ContentMarginLeft = 0;
        s.ContentMarginRight = 0;
        s.ContentMarginTop = 0;
        s.ContentMarginBottom = 0;
        return s;
    }

    private Control BuildShufflePreviewBanner()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var title = new Label();
        title.Text = STS2AdvisorI18n.Pick(
            "≈ PREDICT — merged reshuffle draw order (if it ran now)",
            "≈ 预测 — 若此刻按游戏逻辑合并洗牌后的抽牌堆顺序");
        title.AddThemeColorOverride("font_color", PreviewBannerColor);
        title.AddThemeFontSizeOverride("font_size", FS(12));
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        box.AddChild(title);

        var note = new Label();
        note.Text = STS2AdvisorI18n.Pick(
            "Uses Rng.Shuffle peek; skips hooks. Same Id+upgrade may sort unstably — may diverge.",
            "使用 Rng.Shuffle 窥视；未模拟 Hook。同名同强化 CompareTo 相同时段序不稳定 — 可能与实际不符。");
        note.AddThemeColorOverride("font_color", DimColor);
        note.AddThemeFontSizeOverride("font_size", FS(10));
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        note.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        box.AddChild(note);

        return box;
    }

    private Control BuildSectionTitle(string text)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color", SubtitleColor);
        lbl.AddThemeFontSizeOverride("font_size", FS(11));
        lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return lbl;
    }

    private static readonly Color SubtitleColor = new(0.84f, 0.66f, 0.42f, 1f);
    private static readonly Color PreviewBannerColor = new(0.98f, 0.80f, 0.40f, 1f);

    private Control BuildSectionSpacer()
    {
        var p = new Panel();
        p.CustomMinimumSize = new Vector2(0, (int)(8 * _scale));
        var st = new StyleBoxFlat();
        st.BgColor = new Color(0.92f, 0.63f, 0.16f, 0.10f);
        p.AddThemeStyleboxOverride("panel", st);
        return p;
    }

    private Control BuildCardRow(int index, CardModel card)
    {
        // 判断卡牌类型
        bool isCurse     = card.Type == CardType.Curse || card.Rarity == CardRarity.Curse;
        bool isColorless = !isCurse && (card.Pool?.IsColorless ?? false);
        bool isUpgraded  = card.IsUpgraded;

        Color textColor = isCurse     ? ColCurse
                        : isColorless ? ColColorless
                        : ColPlayer;

        Color bgColor   = isCurse     ? BgCurse
                        : isColorless ? BgColorless
                        : BgPlayer;

        if (isUpgraded) textColor = ColUpgraded;

        var row = new PanelContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.CustomMinimumSize = new Vector2(Mathf.Max(180, (int)(230 * _scale)), 0);

        var rowStyle = new StyleBoxFlat();
        rowStyle.BgColor = bgColor;
        rowStyle.SetCornerRadiusAll(7);
        rowStyle.ContentMarginLeft   = 10;
        rowStyle.ContentMarginRight  = 10;
        rowStyle.ContentMarginTop    = 6;
        rowStyle.ContentMarginBottom = 6;
        rowStyle.BorderColor = new Color(0.92f, 0.63f, 0.16f, 0.24f);
        rowStyle.SetBorderWidthAll(1);
        row.AddThemeStyleboxOverride("panel", rowStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(hbox);

        // 序号
        var idxLbl = new Label();
        idxLbl.Text = $"{index}.";
        idxLbl.AddThemeColorOverride("font_color", DimColor);
        idxLbl.AddThemeFontSizeOverride("font_size", FS(11));
        idxLbl.CustomMinimumSize = new Vector2((int)(28 * _scale), 0);
        hbox.AddChild(idxLbl);

        // 卡名
        var nameLbl = new Label();
        nameLbl.Text = LocText.Of(card) + (isUpgraded ? " +" : "");
        nameLbl.AddThemeColorOverride("font_color", textColor);
        nameLbl.AddThemeFontSizeOverride("font_size", FS(13));
        nameLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLbl.HorizontalAlignment = HorizontalAlignment.Left;
        nameLbl.AutowrapMode = TextServer.AutowrapMode.Off;
        hbox.AddChild(nameLbl);

        // 稀有度小标
        var rarityLbl = new Label();
        rarityLbl.Text = RarityIcon(card.Rarity);
        rarityLbl.AddThemeColorOverride("font_color", RarityColor(card.Rarity));
        rarityLbl.AddThemeFontSizeOverride("font_size", FS(11));
        rarityLbl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        hbox.AddChild(rarityLbl);

        return row;
    }

    // ── 稀有度辅助 ───────────────────────────────────────────
    private static string RarityIcon(CardRarity r) => r switch
    {
        CardRarity.Common   => "●",
        CardRarity.Uncommon => "◆",
        CardRarity.Rare     => "★",
        CardRarity.Curse    => "☠",
        CardRarity.Event    => "✦",
        _                   => ""
    };

    private static Color RarityColor(CardRarity r) => r switch
    {
        CardRarity.Common   => new Color(0.65f, 0.65f, 0.65f),
        CardRarity.Uncommon => new Color(0.40f, 0.75f, 1.00f),
        CardRarity.Rare     => new Color(1.00f, 0.80f, 0.20f),
        CardRarity.Curse    => new Color(0.75f, 0.40f, 1.00f),
        CardRarity.Event    => new Color(0.90f, 0.60f, 1.00f),
        _                   => new Color(0.5f, 0.5f, 0.5f)
    };

    // ── 输入 ─────────────────────────────────────────────────
    public override void _Input(InputEvent evt)
    {
        if (evt is InputEventKey key &&
            key.Pressed &&
            key.Keycode == HotkeyConfig.GetKey(HotkeyAction.AdvisorPanelToggle))
        {
            if (_root != null)
            {
                _root.Visible = !_root.Visible;
                if (_root.Visible)
                {
                    _runtime.ResetCache();
                    UpdateDrawPile(force: true);
                }
            }
        }

        if (_root == null || !_root.Visible) return;

        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            var local  = _root.GetLocalMousePosition();
            bool inHdr = local.Y < 44 && local.X >= 0 && local.X <= _root.Size.X;
            if (mb.Pressed && inHdr)
            {
                if (!PanelDragState.TryStart(nameof(AdvisorPanel))) return;
                _dragging = true;
                _dragOffset = _root.Position - GetViewport().GetMousePosition();
            }
            else if (!mb.Pressed && _dragging)
            {
                _dragging = false;
                PanelDragState.End(nameof(AdvisorPanel));
            }
        }

        if (evt is InputEventMouseMotion && _dragging)
            _root.Position = GetViewport().GetMousePosition() + _dragOffset;
    }

    // ── StyleBox ─────────────────────────────────────────────
    private static StyleBoxFlat MakeRootStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = BgColor;
        s.SetCornerRadiusAll(14);
        s.BorderColor = BorderColor;
        s.SetBorderWidthAll(1);
        s.ShadowColor  = new Color(0.62f, 0.05f, 0.06f, 0.36f);
        s.ShadowSize   = 14;
        s.ShadowOffset = new Vector2(0, 4);
        return s;
    }

    private static StyleBoxFlat MakeHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.20f, 0.10f, 0.08f, 0.58f);
        s.CornerRadiusTopLeft  = 14;
        s.CornerRadiusTopRight = 14;
        s.ContentMarginLeft    = 14;
        s.ContentMarginRight   = 10;
        s.ContentMarginTop     = 11;
        s.ContentMarginBottom  = 11;
        return s;
    }
}