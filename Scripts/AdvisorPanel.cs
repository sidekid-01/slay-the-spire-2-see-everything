using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace STS2Advisor.Scripts;

public partial class AdvisorPanel : CanvasLayer
{
    private PanelContainer?  _root;
    private VBoxContainer?   _cardList;
    private Label?           _header;
    private ScrollContainer? _scroll;
    private Tween?           _tween;

    private bool    _dragging;
    private Vector2 _dragOffset;
    private float   _scale    = 1.0f;
    private const float MinScale = 0.5f;
    private const float MaxScale = 2.0f;

    // ── 字号辅助 ─────────────────────────────────────────────
    private int FS(int b) => Mathf.Max(8, (int)(b * _scale));

    // ── 颜色 ─────────────────────────────────────────────────
    private static readonly Color BgColor      = new(0.045f, 0.055f, 0.09f, 0.94f);
    private static readonly Color BorderColor  = new(1f, 1f, 1f, 0.14f);
    private static readonly Color TitleColor   = new(0.97f, 0.98f, 1f, 0.98f);
    private static readonly Color DimColor     = new(0.62f, 0.66f, 0.75f, 1f);

    // 卡牌颜色
    private static readonly Color ColPlayer    = new(0.35f, 0.95f, 0.55f, 1f);  // 绿色：角色牌
    private static readonly Color ColColorless = new(0.90f, 0.90f, 0.90f, 1f);  // 白色：无色牌
    private static readonly Color ColCurse     = new(0.75f, 0.40f, 1.00f, 1f);  // 紫色：诅咒牌
    private static readonly Color ColUpgraded  = new(0.40f, 0.85f, 1.00f, 1f);  // 蓝色：已升级

    // 卡牌背景
    private static readonly Color BgPlayer     = new(0.06f, 0.22f, 0.11f, 0.62f);
    private static readonly Color BgColorless  = new(0.14f, 0.14f, 0.18f, 0.62f);
    private static readonly Color BgCurse      = new(0.17f, 0.07f, 0.24f, 0.62f);

    // ── Godot 生命周期 ───────────────────────────────────────
    public override void _Ready()
    {
        Layer = 99;
        BuildUi();
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
    }

    private void OnCombatStateChanged(CombatState _) => UpdateDrawPile();

    // ── 缩放（重建 UI 避免模糊）─────────────────────────────
    private void ChangeScale(float delta)
    {
        _scale = Mathf.Clamp(_scale + delta, MinScale, MaxScale);
        var pos = _root?.Position ?? new Vector2(10, 100);
        var visible = _root?.Visible ?? true;
        _root?.QueueFree();
        _root = null;
        BuildUi();
        if (_root != null)
        {
            _root.Position = pos;
            _root.Visible  = visible;
        }
        UpdateDrawPile();
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
        dotStyle.BgColor = new Color(0.35f, 0.95f, 0.55f, 1f);
        dotStyle.SetCornerRadiusAll(4);
        dot.AddThemeStyleboxOverride("panel", dotStyle);
        headerBox.AddChild(dot);

        _header = new Label();
        _header.Text = STS2AdvisorI18n.Pick("Gate of Babylon  (0)", "Gate of Babylon  (0)");
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
        sepStyle.BgColor = new Color(1, 1, 1, 0.08f);
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
        _scroll.CustomMinimumSize    = new Vector2(0, (int)(480 * _scale));
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _scroll.SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill;
        outer.AddChild(_scroll);

        var listPad = new MarginContainer();
        listPad.AddThemeConstantOverride("margin_left",   8);
        listPad.AddThemeConstantOverride("margin_right",  8);
        listPad.AddThemeConstantOverride("margin_top",    6);
        listPad.AddThemeConstantOverride("margin_bottom", 8);
        listPad.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        _cardList = new VBoxContainer();
        _cardList.AddThemeConstantOverride("separation", 5);
        _cardList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        listPad.AddChild(_cardList);
        _scroll.AddChild(listPad);
    }

    // ── 抽牌堆更新 ───────────────────────────────────────────
    public void UpdateDrawPile()
    {
        if (_cardList == null || _header == null) return;

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null) return;

        foreach (var child in _cardList.GetChildren())
            child.QueueFree();

        var player = state.Players.FirstOrDefault();
        if (player == null) return;

        var drawPile = player.PlayerCombatState?.DrawPile.Cards;
        if (drawPile == null) return;

        _header.Text = STS2AdvisorI18n.Pick($"Gate of Babylon  ({drawPile.Count})", $"Gate of Babylon ({drawPile.Count})");

        for (int i = 0; i < drawPile.Count; i++)
        {
            var card = drawPile[i];
            _cardList.AddChild(BuildCardRow(i + 1, card));
        }
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
        rowStyle.BorderColor = new Color(1f, 1f, 1f, 0.10f);
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
            if (_root != null) _root.Visible = !_root.Visible;
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
        s.ShadowColor  = new Color(0, 0, 0, 0.45f);
        s.ShadowSize   = 16;
        s.ShadowOffset = new Vector2(0, 5);
        return s;
    }

    private static StyleBoxFlat MakeHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(1, 1, 1, 0.04f);
        s.CornerRadiusTopLeft  = 14;
        s.CornerRadiusTopRight = 14;
        s.ContentMarginLeft    = 14;
        s.ContentMarginRight   = 10;
        s.ContentMarginTop     = 11;
        s.ContentMarginBottom  = 11;
        return s;
    }
}