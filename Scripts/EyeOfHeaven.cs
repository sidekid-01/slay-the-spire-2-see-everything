using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace STS2Advisor.Scripts;

// ============================================================
//  数据结构
// ============================================================

public record EventPrediction(string Label, string Value, PredictionTag Tag = PredictionTag.Normal);
public enum PredictionTag { Normal, Good, Warning, Bad }

// ============================================================
//  预测器接口 & 注册表
// ============================================================

public interface IEventPredictor
{
    Type EventType { get; }
    List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng);
}

public static class EventPredictorRegistry
{
    public static readonly List<IEventPredictor> All = BuildAll().ToList();

    private static readonly Dictionary<Type, IEventPredictor> _lookup =
        All.ToDictionary(p => p.EventType);

    public static IEventPredictor? GetFor(Type eventType) =>
        _lookup.GetValueOrDefault(eventType);

    private static IEnumerable<IEventPredictor> BuildAll()
    {
        // Complex/special events.
        yield return new DollRoomPredictor();
        yield return new EndlessConveyorPredictor();
        yield return new TrashHeapPredictor();
        yield return new SelfHelpBookPredictor();
        yield return new BattlewornDummyPredictor();
        yield return new DoorsOfLightAndDarkPredictor();
        yield return new NeowPredictor();

        // Generic transform events from catalog.
        foreach (var predictor in TransformEventCatalog.CreatePredictors())
            yield return predictor;
    }
}

// ============================================================
//  Harmony Patch 共用逻辑
// ============================================================

internal static class EventAdvisorPatchCore
{
    internal static void RunPrediction(EventModel instance)
    {
        try
        {
            if (instance.Owner == null) return;

            var predictor = EventPredictorRegistry.GetFor(instance.GetType());

            uint seed = (uint)(
                instance.Owner.RunState.Rng.Seed
                + instance.Owner.NetId
                + (ulong)StringHelper.GetDeterministicHashCode(instance.Id.Entry)
            );
            var mirrorRng   = new Rng(seed);
            var predictions = predictor != null
                ? predictor.Predict(instance, mirrorRng)
                : EventFallbackPredictor.Predict(instance);

            EventAdvisorPanel.Instance?.ShowPredictions(
                EventPredictionText.EventDisplayName(instance), predictions);

            Log.Debug($"===== 事件预测：{instance.GetType().Name} =====");
            foreach (var p in predictions)
                Log.Debug($"  {p.Label}：{p.Value}");
        }
        catch (Exception e)
        {
            Log.Debug($"[EyeOfHeaven] 预测出错: {e}");
            EventAdvisorPanel.Instance?.ShowPredictions(
                instance.GetType().Name,
                new System.Collections.Generic.List<EventPrediction>
                {
                    new(
                        STS2AdvisorI18n.Pick("Prediction error", "预测出错"),
                        $"{e.GetType().Name}: {e.Message}",
                        PredictionTag.Bad)
                });
        }
    }
}

// 普通事件
[HarmonyPatch(typeof(EventModel), "BeginEvent")]
public static class EventAdvisorPatch
{
    [HarmonyPostfix]
    public static void Postfix(EventModel __instance)
        => EventAdvisorPatchCore.RunPrediction(__instance);
}

// ============================================================
//  UI 面板（F1 呼出）
// ============================================================

public partial class EventAdvisorPanel : CanvasLayer
{
    public static EventAdvisorPanel? Instance { get; private set; }

    private PanelContainer? _root;
    private Label?          _eventNameLabel;
    private VBoxContainer?  _itemContainer;
    private Tween?          _tween;

    private bool    _dragging;
    private Vector2 _dragOffset;
    private float   _scale = 1.0f;
    private const float MinScale = 0.5f;
    private const float MaxScale = 2.0f;

    // 缓存上一次预测结果，供缩放后重填
    private List<EventPrediction>? _lastPredictions;
    private string                 _lastEventName = "";

    // ── 字号辅助（避免模糊的关键）──────────────────────────────
    private int FS(int b) => Mathf.Max(8, (int)(b * _scale));

    // ── 颜色常量 ────────────────────────────────────────────────
    private static readonly Color BgColor      = new(0.08f, 0.08f, 0.12f, 0.92f);
    private static readonly Color BorderColor  = new(1f,    1f,    1f,    0.10f);
    private static readonly Color TitleColor   = new(1f,    1f,    1f,    0.95f);
    private static readonly Color SubtitleColor= new(0.6f,  0.6f,  0.7f,  1f);
    private static readonly Color LabelColor   = new(0.55f, 0.55f, 0.65f, 1f);

    private static readonly Color GoodBg    = new(0.13f, 0.55f, 0.35f, 0.30f);
    private static readonly Color WarningBg = new(0.70f, 0.50f, 0.10f, 0.28f);
    private static readonly Color BadBg     = new(0.60f, 0.15f, 0.15f, 0.28f);
    private static readonly Color NormalBg  = new(1f,    1f,    1f,    0.05f);

    private static readonly Color GoodText    = new(0.40f, 0.95f, 0.65f, 1f);
    private static readonly Color WarningText = new(1.00f, 0.80f, 0.30f, 1f);
    private static readonly Color BadText     = new(1.00f, 0.45f, 0.45f, 1f);
    private static readonly Color NormalText  = new(0.92f, 0.92f, 0.95f, 1f);

    // ── Godot 生命周期 ───────────────────────────────────────────
    public override void _Ready()
    {
        Instance = this;
        Layer    = 100;
        BuildUi();
        SetVisible(false);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公开接口 ─────────────────────────────────────────────────
    public void ShowPredictions(string eventName, List<EventPrediction> predictions)
    {
        // 缓存，供缩放后重填
        _lastEventName   = eventName;
        _lastPredictions = predictions;

        RefillContent(eventName, predictions);
        SetVisible(true);
        AnimateIn();
    }

    public new void Hide() => SetVisible(false);

    // ── 缩放：重建 UI 而非 Scale 节点，避免模糊 ──────────────────
    private void ChangeScale(float delta)
    {
        _scale = Mathf.Clamp(_scale + delta, MinScale, MaxScale);

        var pos     = _root?.Position ?? new Vector2(16, 120);
        var visible = _root?.Visible  ?? false;
        _tween?.Kill();
        _root?.QueueFree();
        _root = null;

        BuildUi();

        if (_root != null)
        {
            _root.Position = pos;
            _root.Visible  = visible;
        }

        // 重填缓存的预测内容
        if (_lastPredictions != null && _lastPredictions.Count > 0)
            RefillContent(_lastEventName, _lastPredictions);
    }

    // ── 填充内容区 ───────────────────────────────────────────────
    private void RefillContent(string eventName, List<EventPrediction> predictions)
    {
        if (_itemContainer == null || _eventNameLabel == null) return;

        foreach (var child in _itemContainer.GetChildren())
            child.QueueFree();

        bool looksLikeTypeName = eventName.All(ch => char.IsLetterOrDigit(ch)) && eventName.Any(char.IsUpper);
        _eventNameLabel.Text = looksLikeTypeName
            ? Regex.Replace(eventName, "([A-Z])", " $1").Trim()
            : eventName;

        foreach (var pred in predictions)
            _itemContainer.AddChild(BuildRow(pred));
    }

    // ── 淡入动画（不再用 Scale 动画，避免模糊）──────────────────
    private void AnimateIn()
    {
        if (_root == null) return;
        _tween?.Kill();
        _tween = CreateTween();
        _root.Modulate = new Color(1, 1, 1, 0);
        _tween.TweenProperty(_root, "modulate", new Color(1, 1, 1, 1), 0.18f)
              .SetTrans(Tween.TransitionType.Cubic)
              .SetEase(Tween.EaseType.Out);
    }

    // ── UI 构建 ──────────────────────────────────────────────────
    private void BuildUi()
    {
        _root = new PanelContainer();
        _root.Position = new Vector2(16, 120);
        _root.CustomMinimumSize = new Vector2((int)(260 * _scale), 0);
        _root.AddThemeStyleboxOverride("panel", MakeRootStyle());
        AddChild(_root);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 0);
        _root.AddChild(outer);

        // 标题栏
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", MakeHeaderStyle());
        outer.AddChild(header);

        var headerBox = new HBoxContainer();
        headerBox.AddThemeConstantOverride("separation", 6);
        header.AddChild(headerBox);

        var dot = new Panel();
        dot.CustomMinimumSize = new Vector2(FS(8), FS(8));
        dot.AddThemeStyleboxOverride("panel", MakeDotStyle());
        dot.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        headerBox.AddChild(dot);

        var textBox = new VBoxContainer();
        textBox.AddThemeConstantOverride("separation", 2);
        textBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerBox.AddChild(textBox);

        var titleLabel = new Label();
        titleLabel.Text = "天堂之眼";
        titleLabel.AddThemeColorOverride("font_color", TitleColor);
        titleLabel.AddThemeFontSizeOverride("font_size", FS(13));
        textBox.AddChild(titleLabel);

        _eventNameLabel = new Label();
        _eventNameLabel.Text = "";
        _eventNameLabel.AddThemeColorOverride("font_color", SubtitleColor);
        _eventNameLabel.AddThemeFontSizeOverride("font_size", FS(11));
        textBox.AddChild(_eventNameLabel);

        foreach (var (txt, action) in new (string, Action)[]
        {
            ("-",  () => ChangeScale(-0.1f)),
            ("+",  () => ChangeScale( 0.1f)),
            ("×",  Hide),
        })
        {
            var btn = new Button();
            btn.Text = txt;
            btn.Flat = true;
            btn.CustomMinimumSize = new Vector2(24, 24);
            btn.AddThemeColorOverride("font_color", SubtitleColor);
            btn.AddThemeFontSizeOverride("font_size", FS(16));
            btn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            btn.Pressed += action;
            headerBox.AddChild(btn);
        }

        // 分割线
        var sep = new Panel();
        sep.CustomMinimumSize = new Vector2(0, 1);
        sep.AddThemeStyleboxOverride("panel", MakeSepStyle());
        outer.AddChild(sep);

        // 内容区
        var contentPad = new MarginContainer();
        contentPad.AddThemeConstantOverride("margin_left",   (int)(12 * _scale));
        contentPad.AddThemeConstantOverride("margin_right",  (int)(12 * _scale));
        contentPad.AddThemeConstantOverride("margin_top",    (int)(10 * _scale));
        contentPad.AddThemeConstantOverride("margin_bottom", (int)(12 * _scale));
        outer.AddChild(contentPad);

        _itemContainer = new VBoxContainer();
        _itemContainer.AddThemeConstantOverride("separation", (int)(6 * _scale));
        contentPad.AddChild(_itemContainer);
    }

    // ── 行构建 ───────────────────────────────────────────────────
    private Control BuildRow(EventPrediction pred)
    {
        var (bgColor, textColor) = pred.Tag switch
        {
            PredictionTag.Good    => (GoodBg,    GoodText),
            PredictionTag.Warning => (WarningBg, WarningText),
            PredictionTag.Bad     => (BadBg,     BadText),
            _                     => (NormalBg,  NormalText)
        };

        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", MakeRowStyle(bgColor));

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", (int)(8 * _scale));
        card.AddChild(hbox);

        var lbl = new Label();
        lbl.Text = pred.Label;
        lbl.AddThemeColorOverride("font_color", LabelColor);
        lbl.AddThemeFontSizeOverride("font_size", FS(11));
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        hbox.AddChild(lbl);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(spacer);

        var val = new Label();
        val.Text = pred.Value;
        val.AddThemeColorOverride("font_color", textColor);
        val.AddThemeFontSizeOverride("font_size", FS(12));
        val.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        val.HorizontalAlignment = HorizontalAlignment.Right;
        val.CustomMinimumSize = new Vector2((int)(110 * _scale), 0);
        hbox.AddChild(val);

        return card;
    }

    // ── 输入 ─────────────────────────────────────────────────────
    public override void _Input(InputEvent evt)
    {
        if (evt is InputEventKey key &&
            key.Pressed &&
            key.Keycode == HotkeyConfig.GetKey(HotkeyAction.EventAdvisorPanelToggle))
        {
            if (_root != null) _root.Visible = !_root.Visible;
        }

        if (_root == null || !_root.Visible) return;

        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            var local     = _root.GetLocalMousePosition();
            bool inHeader = local.Y < 44 && local.X >= 0 && local.X <= _root.Size.X;
            if (mb.Pressed && inHeader)
            {
                if (!PanelDragState.TryStart(nameof(EventAdvisorPanel))) return;
                _dragging = true;
                _dragOffset = _root.Position - GetViewport().GetMousePosition();
            }
            else if (!mb.Pressed && _dragging)
            {
                _dragging = false;
                PanelDragState.End(nameof(EventAdvisorPanel));
            }
        }

        if (evt is InputEventMouseMotion && _dragging)
            _root.Position = GetViewport().GetMousePosition() + _dragOffset;
    }

    // ── StyleBox ─────────────────────────────────────────────────
    private static StyleBoxFlat MakeRootStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor     = BgColor;
        s.SetCornerRadiusAll(14);
        s.BorderColor = BorderColor;
        s.SetBorderWidthAll(1);
        s.ShadowColor  = new Color(0, 0, 0, 0.45f);
        s.ShadowSize   = 12;
        s.ShadowOffset = new Vector2(0, 4);
        return s;
    }

    private static StyleBoxFlat MakeHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor              = new Color(1, 1, 1, 0.04f);
        s.CornerRadiusTopLeft  = 14;
        s.CornerRadiusTopRight = 14;
        s.ContentMarginLeft    = 14;
        s.ContentMarginRight   = 10;
        s.ContentMarginTop     = 12;
        s.ContentMarginBottom  = 12;
        return s;
    }

    private static StyleBoxFlat MakeDotStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.40f, 0.85f, 0.60f, 1f);
        s.SetCornerRadiusAll(4);
        return s;
    }

    private static StyleBoxFlat MakeSepStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(1, 1, 1, 0.08f);
        return s;
    }

    private static StyleBoxFlat MakeRowStyle(Color bg)
    {
        var s = new StyleBoxFlat();
        s.BgColor             = bg;
        s.SetCornerRadiusAll(8);
        s.ContentMarginLeft   = 10;
        s.ContentMarginRight  = 10;
        s.ContentMarginTop    = 7;
        s.ContentMarginBottom = 7;
        return s;
    }
}