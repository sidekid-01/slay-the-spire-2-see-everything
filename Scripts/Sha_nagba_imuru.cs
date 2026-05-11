using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace STS2Advisor.Scripts;

// ============================================================
//  全知全能之星 Sha_Nagba_Imuru
//  地图节点完整探查面板，F4 呼出/隐藏
// ============================================================

public partial class Sha_Nagba_Imuru : CanvasLayer
{
    public static Sha_Nagba_Imuru? Instance { get; private set; }

    private PanelContainer?  _root;
    private Label?           _titleLabel;
    private VBoxContainer?   _content;
    private ScrollContainer? _scroll;
    private Label?           _statusLabel;

    private bool    _dragging;
    private Vector2 _dragOffset;
    private float   _scale    = 1.0f;
    private const float MinScale = 0.5f;
    private const float MaxScale = 2.0f;

    // 基准字号，乘以 _scale 后使用
    private int FS(int base_) => Mathf.Max(8, (int)(base_ * _scale));

    // ── 颜色 ─────────────────────────────────────────────────
    private static readonly Color ColBg      = new(0.04f, 0.05f, 0.09f, 0.93f);
    private static readonly Color ColBorder  = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColTitle   = new(1.0f, 0.92f, 0.55f, 1f);
    private static readonly Color ColDim     = new(0.45f, 0.45f, 0.50f, 1f);
    private static readonly Color ColCurrent = new(0.25f, 1.0f, 0.45f, 1f);
    private static readonly Color ColVisited = new(0.30f, 0.30f, 0.33f, 1f);
    private static readonly Color ColNext    = new(1.0f, 0.85f, 0.25f, 1f);
    private static readonly Color ColUnknown = new(0.70f, 0.50f, 1.00f, 1f);

    private static readonly Dictionary<MapPointType, (string icon, Color color)> PointStyle = new()
    {
        [MapPointType.Monster]    = ("⚔",  new Color(0.95f, 0.40f, 0.40f)),
        [MapPointType.Elite]      = ("💀", new Color(1.00f, 0.25f, 0.25f)),
        [MapPointType.Boss]       = ("👑", new Color(1.00f, 0.15f, 0.15f)),
        [MapPointType.RestSite]   = ("🔥", new Color(0.35f, 0.75f, 1.00f)),
        [MapPointType.Shop]       = ("💰", new Color(1.00f, 0.85f, 0.20f)),
        [MapPointType.Treasure]   = ("📦", new Color(0.90f, 0.75f, 0.30f)),
        [MapPointType.Unknown]    = ("❓", new Color(0.70f, 0.50f, 1.00f)),
        [MapPointType.Ancient]    = ("🌀", new Color(0.45f, 1.00f, 0.75f)),
        [MapPointType.Unassigned] = ("？", new Color(0.35f, 0.35f, 0.35f)),
    };

    // ── 反射缓存 ─────────────────────────────────────────────
    private static readonly FieldInfo? RoomsField =
        typeof(ActModel).GetField("_rooms", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? NonEventOddsField =
        typeof(UnknownMapPointOdds).GetField("_nonEventOdds", BindingFlags.NonPublic | BindingFlags.Instance);

    // ── 预测用的模拟指针（不影响游戏状态）───────────────────
    private record struct SimCounters(int Events, int Normals, int Elites);

    // ── Godot 生命周期 ───────────────────────────────────────
    public override void _Ready()
    {
        Instance = this;
        Layer    = 101;
        BuildUi();
        if (_root != null) _root.Visible = false;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ── 刷新入口 ─────────────────────────────────────────────
    public void Refresh()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state == null) { SetStatus(STS2AdvisorI18n.Pick("Not currently in a run", "当前不在游戏中")); return; }
            RebuildContent(state);
            if (_root != null) _root.Visible = true;
        }
        catch (Exception e)
        {
            Log.Debug($"[Sha_Nagba_Imuru] 刷新出错: {e.Message}\n{e.StackTrace}");
            SetStatus(STS2AdvisorI18n.Pick("Error: ", "出错: ") + e.Message);
        }
    }

    // ── 核心渲染 ─────────────────────────────────────────────

    private void RebuildContent(RunState state)
    {
        if (_content == null) return;
        foreach (var c in _content.GetChildren()) c.QueueFree();

        var act    = state.Act;
        var actMap = state.Map;
        var rooms  = GetRoomSet(act);

        if (_titleLabel != null)
            _titleLabel.Text = STS2AdvisorI18n.Pick(
                $"All-Seeing Star · Act {state.CurrentActIndex + 1} [{act.Id.Entry}]",
                $"全知全能之星 · 第{state.CurrentActIndex + 1}幕 [{act.Id.Entry}]");

        if (rooms == null)
        {
            AddLine(STS2AdvisorI18n.Pick("Failed to read RoomSet", "无法读取 RoomSet"), ColDim);
            return;
        }

        // ── 队列总览 ────────────────────────────────────────
        AddSectionHeader(STS2AdvisorI18n.Pick("━━ Queue Overview ━━", "━━ 队列总览 ━━"));
        AddKeyValue(STS2AdvisorI18n.Pick("Next event", "下一事件"), LocText.Of(rooms.NextEvent), ColNext);
        AddKeyValue(STS2AdvisorI18n.Pick("Next normal fight", "下一普通战"), LocText.Of(rooms.NextNormalEncounter), new Color(0.95f, 0.45f, 0.45f));
        AddKeyValue(STS2AdvisorI18n.Pick("Next elite fight", "下一精英战"), LocText.Of(rooms.NextEliteEncounter), new Color(1.0f, 0.25f, 0.25f));
        AddKeyValue(STS2AdvisorI18n.Pick("Boss", "首领"), SafeGet(() => rooms.Boss.Id.Entry + (rooms.HasSecondBoss ? $" / {rooms.SecondBoss!.Id.Entry}" : "")), new Color(1.0f, 0.2f, 0.2f));
        AddKeyValue(STS2AdvisorI18n.Pick("Event progress", "事件进度"), $"{rooms.eventsVisited}/{rooms.events.Count}", ColDim);
        AddKeyValue(STS2AdvisorI18n.Pick("Normal fight progress", "普通战进度"), $"{rooms.normalEncountersVisited}/{rooms.normalEncounters.Count}", ColDim);
        AddKeyValue(STS2AdvisorI18n.Pick("Elite fight progress", "精英战进度"), $"{rooms.eliteEncountersVisited}/{rooms.eliteEncounters.Count}", ColDim);

        // Unknown 节点当前概率
        var unknownOdds = GetUnknownOdds(state);
        if (unknownOdds != null)
        {
            AddLine(STS2AdvisorI18n.Pick("Unknown node odds:", "未知节点当前概率："), ColUnknown, 10);
            foreach (var (rt, pct) in unknownOdds)
                AddLine($"  {rt}: {pct:F0}%", ColUnknown, 10);
        }

        // ── 地图节点预测 ────────────────────────────────────
        AddSectionHeader(STS2AdvisorI18n.Pick("━━ Map Node Prediction ━━", "━━ 地图节点预测 ━━"));

        var currentCoord  = state.CurrentMapCoord;
        var visitedCoords = new HashSet<MapCoord>(state.VisitedMapCoords);

        // 构建已访问坐标 → 实际 RoomType 的映射
        // 用于修正 Unknown 节点实际消耗了哪个队列
        var visitedRoomTypes = BuildVisitedRoomTypes(state);

        // 模拟指针：从 0 开始重新推算，经过每个已访问节点时按实际类型推进
        // 这样能正确处理 Unknown 节点实际变成精英/战斗/商店的情况
        var sim = new SimCounters(0, 0, 0);

        // 先用已访问节点把 sim 推到当前状态
        foreach (var coord in state.VisitedMapCoords)
        {
            // 当前节点还没有离开，不推进
            if (currentCoord.HasValue && coord == currentCoord.Value) break;

            var visitedPoint = actMap.GetPoint(coord);
            if (visitedPoint == null) continue;

            // 用实际 RoomType（而非 MapPointType）来推进
            if (visitedRoomTypes.TryGetValue(coord, out RoomType actualType))
                AdvanceSimByRoomType(actualType, ref sim);
            else
                AdvanceSimByPointType(visitedPoint.PointType, ref sim);
        }

        // Boss 行
        AddRowHeader(STS2AdvisorI18n.Pick("★ Boss", "★ 首领"), 1);
        AddPointRowHorizontal(
            new[] { actMap.BossMapPoint },
            currentCoord, visitedCoords, rooms, ref sim, isBoss: true);

        // 普通行从高到低
        int rowCount = actMap.GetRowCount();
        int displayRow = 2;
        for (int row = rowCount - 1; row >= 0; row--)
        {
            var pts = actMap.GetPointsInRow(row).OrderBy(p => p.coord.col).ToList();
            if (pts.Count == 0) continue;
            AddRowHeader(STS2AdvisorI18n.Pick($"Row {displayRow}  (Map row {row}, {pts.Count} nodes)", $"第 {displayRow} 行  (地图行 {row}，共 {pts.Count} 个节点)"), displayRow);
            AddPointRowHorizontal(pts, currentCoord, visitedCoords, rooms, ref sim);
            displayRow++;
        }

        // 起始行
        AddRowHeader(STS2AdvisorI18n.Pick("★ Start / Ancient", "★ 起始 / 古代"), displayRow);
        AddPointRowHorizontal(
            new[] { actMap.StartingMapPoint },
            currentCoord, visitedCoords, rooms, ref sim);

        // ── 完整队列 ────────────────────────────────────────
        AddSectionHeader(STS2AdvisorI18n.Pick("━━ Full Event Queue ━━", "━━ 完整事件队列 ━━"));
        AddQueueList(rooms.events.Select(LocText.Of).ToList(),
            rooms.eventsVisited, ColNext);

        AddSectionHeader(STS2AdvisorI18n.Pick("━━ Full Normal Fight Queue ━━", "━━ 完整普通战队列 ━━"));
        AddQueueList(rooms.normalEncounters.Select(LocText.Of).ToList(),
            rooms.normalEncountersVisited, new Color(0.95f, 0.45f, 0.45f));

        AddSectionHeader(STS2AdvisorI18n.Pick("━━ Full Elite Fight Queue ━━", "━━ 完整精英战队列 ━━"));
        AddQueueList(rooms.eliteEncounters.Select(LocText.Of).ToList(),
            rooms.eliteEncountersVisited, new Color(1.0f, 0.25f, 0.25f));
    }

    // ── 行标题（醒目分隔）────────────────────────────────────

    private void AddRowHeader(string text, int rowIndex)
    {
        if (_content == null) return;

        // 带背景色的醒目标题行
        Color bgColor = rowIndex == 1
            ? new Color(0.25f, 0.05f, 0.05f, 0.8f)   // Boss 红色背景
            : new Color(0.05f, 0.12f, 0.22f, 0.85f);  // 普通行蓝色背景

        var card = new PanelContainer();
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var headerStyle = new StyleBoxFlat();
        headerStyle.BgColor = bgColor;
        headerStyle.SetCornerRadiusAll(4);
        headerStyle.ContentMarginLeft = 10;
        headerStyle.ContentMarginRight = 10;
        headerStyle.ContentMarginTop = 5;
        headerStyle.ContentMarginBottom = 5;
        headerStyle.BorderColor = new Color(1f, 1f, 1f, 0.15f);
        headerStyle.SetBorderWidthAll(1);
        card.AddThemeStyleboxOverride("panel", headerStyle);

        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color",
            rowIndex == 1 ? new Color(1.0f, 0.5f, 0.5f) : ColTitle);
        lbl.AddThemeFontSizeOverride("font_size", FS(13));
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.AddChild(lbl);

        // 上方留一点间距
        var spacer = new MarginContainer();
        spacer.AddThemeConstantOverride("margin_top", 8);
        spacer.AddChild(card);
        _content.AddChild(spacer);
    }

    // ── 横排一整行节点 ───────────────────────────────────────

    private void AddPointRowHorizontal(
        IEnumerable<MapPoint> points,
        MapCoord? currentCoord,
        HashSet<MapCoord> visitedCoords,
        RoomSet rooms,
        ref SimCounters sim,
        bool isBoss = false)
    {
        if (_content == null) return;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var ptList = points.ToList();
        for (int i = 0; i < ptList.Count; i++)
        {
            var card = BuildPointCardWithIndex(
                ptList[i], i + 1, ptList.Count,
                currentCoord, visitedCoords, rooms, ref sim, isBoss);
            hbox.AddChild(card);
        }

        _content.AddChild(hbox);
    }

    // ── 带序号的节点卡片 ─────────────────────────────────────

    private Control BuildPointCardWithIndex(
        MapPoint point,
        int colIndex,
        int totalInRow,
        MapCoord? currentCoord,
        HashSet<MapCoord> visitedCoords,
        RoomSet rooms,
        ref SimCounters sim,
        bool isBoss = false)
    {
        bool isCurrent = currentCoord.HasValue && point.coord == currentCoord.Value;
        bool isVisited = visitedCoords.Contains(point.coord);

        var (icon, typeColor) = PointStyle.GetValueOrDefault(point.PointType, ("?", ColDim));
        Color displayColor = isVisited ? ColVisited : isCurrent ? ColCurrent : typeColor;

        Color bgColor = isCurrent
            ? new Color(0.06f, 0.22f, 0.09f, 0.85f)
            : isVisited
                ? new Color(0.06f, 0.06f, 0.06f, 0.55f)
                : new Color(0.06f, 0.06f, 0.14f, 0.70f);

        var card = new PanelContainer();
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.AddThemeStyleboxOverride("panel", MakeCardStyle(bgColor));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        card.AddChild(vbox);

        // 序号标注
        var indexLbl = new Label();
        indexLbl.Text = totalInRow > 1
            ? $"[{colIndex}/{totalInRow}] ({point.coord.col},{point.coord.row})"
            : $"({point.coord.col},{point.coord.row})";
        indexLbl.AddThemeColorOverride("font_color", new Color(0.50f, 0.50f, 0.55f));
        indexLbl.AddThemeFontSizeOverride("font_size", FS(11));
        vbox.AddChild(indexLbl);

        // 图标 + 类型
        var typeRow = new HBoxContainer();
        typeRow.AddThemeConstantOverride("separation", 3);
        vbox.AddChild(typeRow);

        var iconLbl = new Label();
        iconLbl.Text = icon;
        iconLbl.AddThemeFontSizeOverride("font_size", FS(14));
        typeRow.AddChild(iconLbl);

        var typeLbl = new Label();
        typeLbl.Text = LocalizePointType(point.PointType) + (isCurrent ? " ◄" : "");
        typeLbl.AddThemeColorOverride("font_color", displayColor);
        typeLbl.AddThemeFontSizeOverride("font_size", FS(12));
        typeLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        typeRow.AddChild(typeLbl);

        // 预测内容
        if (!isVisited)
        {
            string? pred = PredictAndAdvance(point.PointType, rooms, ref sim);
            if (pred != null)
            {
                var predLbl = new Label();
                predLbl.Text = pred;
                predLbl.AddThemeColorOverride("font_color",
                    isCurrent ? new Color(0.8f, 1.0f, 0.6f) : new Color(0.75f, 0.75f, 0.5f));
                predLbl.AddThemeFontSizeOverride("font_size", FS(11));
                predLbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                predLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                vbox.AddChild(predLbl);
            }
        }

        return card;
    }

    // ── 原 AddPointCard 保留（用于兼容）────────────────────────

    private void AddPointCard(
        MapPoint point,
        MapCoord? currentCoord,
        HashSet<MapCoord> visitedCoords,
        RoomSet rooms,
        ref SimCounters sim,
        bool isBoss = false)
    {
        if (_content == null) return;

        bool isCurrent = currentCoord.HasValue && point.coord == currentCoord.Value;
        bool isVisited = visitedCoords.Contains(point.coord);

        var (icon, typeColor) = PointStyle.GetValueOrDefault(point.PointType, ("?", ColDim));
        Color displayColor = isVisited ? ColVisited : isCurrent ? ColCurrent : typeColor;

        Color bgColor = isCurrent
            ? new Color(0.06f, 0.20f, 0.09f, 0.8f)
            : isVisited
                ? new Color(0.06f, 0.06f, 0.06f, 0.5f)
                : new Color(0.06f, 0.06f, 0.13f, 0.65f);

        var card = new PanelContainer();
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.AddThemeStyleboxOverride("panel", MakeCardStyle(bgColor));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        card.AddChild(vbox);

        // 标题行
        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(titleRow);

        var iconLbl = new Label();
        iconLbl.Text = icon;
        iconLbl.AddThemeFontSizeOverride("font_size", FS(13));
        titleRow.AddChild(iconLbl);

        var typeLbl = new Label();
        typeLbl.Text = LocalizePointType(point.PointType)
            + $"  ({point.coord.col},{point.coord.row})"
            + (isCurrent ? STS2AdvisorI18n.Pick("  ◄ Current", "  ◄ 当前") : "");
        typeLbl.AddThemeColorOverride("font_color", displayColor);
        typeLbl.AddThemeFontSizeOverride("font_size", FS(12));
        typeLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleRow.AddChild(typeLbl);

        // 预测内容（未访问节点）
        if (!isVisited)
        {
            string? pred = PredictAndAdvance(point.PointType, rooms, ref sim);
            if (pred != null)
            {
                var predLbl = new Label();
                predLbl.Text = pred;
                predLbl.AddThemeColorOverride("font_color",
                    isCurrent ? new Color(0.8f, 1.0f, 0.6f) : new Color(0.75f, 0.75f, 0.5f));
                predLbl.AddThemeFontSizeOverride("font_size", FS(11));
                predLbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                predLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                vbox.AddChild(predLbl);
            }
        }

        _content.AddChild(card);
    }

    // ── 构建已访问坐标 → 实际 RoomType 映射 ─────────────────

    private static Dictionary<MapCoord, RoomType> BuildVisitedRoomTypes(RunState state)
    {
        var result = new Dictionary<MapCoord, RoomType>();
        try
        {
            int actIdx = state.CurrentActIndex;
            if (actIdx >= state.MapPointHistory.Count) return result;

            var actHistory = state.MapPointHistory[actIdx];
            var coords     = state.VisitedMapCoords;

            for (int i = 0; i < Math.Min(actHistory.Count, coords.Count); i++)
            {
                var entry = actHistory[i];
                if (entry.Rooms.Count > 0)
                    result[coords[i]] = entry.Rooms[0].RoomType;
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[Sha_Nagba_Imuru] BuildVisitedRoomTypes 失败: {e.Message}");
        }
        return result;
    }

    // ── 按实际 RoomType 推进模拟指针 ────────────────────────

    private static void AdvanceSimByRoomType(RoomType rt, ref SimCounters sim)
    {
        switch (rt)
        {
            case RoomType.Monster: sim = sim with { Normals = sim.Normals + 1 }; break;
            case RoomType.Elite:   sim = sim with { Elites  = sim.Elites  + 1 }; break;
            case RoomType.Event:   sim = sim with { Events  = sim.Events  + 1 }; break;
            // Shop/Treasure/RestSite/Boss 不消耗这三条队列
        }
    }

    // ── 按 MapPointType 推进（未访问节点的后备方案）──────────

    private static void AdvanceSimByPointType(MapPointType pt, ref SimCounters sim)
    {
        switch (pt)
        {
            case MapPointType.Monster: sim = sim with { Normals = sim.Normals + 1 }; break;
            case MapPointType.Elite:   sim = sim with { Elites  = sim.Elites  + 1 }; break;
            case MapPointType.Unknown: sim = sim with { Events  = sim.Events  + 1 }; break;
        }
    }

    // ── 模拟指针推进（关键逻辑）─────────────────────────────
    // 按节点类型消耗对应队列的一格，返回预测内容字符串

    private static string? PredictAndAdvance(
        MapPointType pt, RoomSet rooms, ref SimCounters sim)
    {
        switch (pt)
        {
            case MapPointType.Monster:
            {
                if (rooms.normalEncounters.Count == 0) return null;
                int idx = sim.Normals % rooms.normalEncounters.Count;
                string name = rooms.normalEncounters[idx].Id.Entry;
                sim = sim with { Normals = sim.Normals + 1 };
                return name;
            }
            case MapPointType.Elite:
            {
                if (rooms.eliteEncounters.Count == 0) return null;
                int idx = sim.Elites % rooms.eliteEncounters.Count;
                string name = rooms.eliteEncounters[idx].Id.Entry;
                sim = sim with { Elites = sim.Elites + 1 };
                return name;
            }
            case MapPointType.Unknown:
            {
                // Unknown 节点消耗一个事件（最可能），但也可能是战斗/商店
                // 显示"事件队列当前项（可能）"并推进
                if (rooms.events.Count == 0) return STS2AdvisorI18n.Pick("❓ Unknown (event pool empty)", "❓ 未知（事件池空）");
                int idx = sim.Events % rooms.events.Count;
                string evtName = LocText.Of(rooms.events[idx]);
                sim = sim with { Events = sim.Events + 1 };
                return STS2AdvisorI18n.Pick($"❓ Maybe: {evtName}", $"❓ 可能: {evtName}");
            }
            case MapPointType.Boss:
            {
                string s = SafeGet(() => rooms.Boss.Id.Entry) ?? "?";
                if (rooms.HasSecondBoss)
                    s += $" / {SafeGet(() => rooms.SecondBoss!.Id.Entry)}";
                return s;
            }
            case MapPointType.Ancient:
                return SafeGet(() => rooms.Ancient.Id.Entry);
            case MapPointType.RestSite:
                return STS2AdvisorI18n.Pick("Rest / Smith", "休息 / 锻造");
            case MapPointType.Shop:
                return STS2AdvisorI18n.Pick("Shop", "商店");
            case MapPointType.Treasure:
                return STS2AdvisorI18n.Pick("Chest", "宝箱");
            default:
                return null;
        }
    }

    // ── Unknown 节点概率读取 ─────────────────────────────────

    private static List<(string name, float pct)>? GetUnknownOdds(RunState state)
    {
        try
        {
            var odds = state.Odds.UnknownMapPoint;
            if (NonEventOddsField?.GetValue(odds) is not Dictionary<RoomType, float> nonEvent)
                return null;

            float total = nonEvent.Values.Where(v => v >= 0).Sum();
            // 剩余概率给 Event
            float eventPct = Math.Max(0f, 1f - total) * 100f;

            var result = new List<(string, float)>();
            result.Add((STS2AdvisorI18n.Pick("Event", "事件"), eventPct));
            foreach (var (rt, v) in nonEvent.OrderByDescending(x => x.Value))
            {
                if (v > 0)
                    result.Add((LocalizeRoomType(rt), v * 100f));
            }
            return result;
        }
        catch { return null; }
    }

    // ── 完整队列列表 ─────────────────────────────────────────

    private void AddQueueList(List<string> items, int visited, Color activeColor)
    {
        if (items.Count == 0) { AddLine(STS2AdvisorI18n.Pick("  (empty)", "  （空）"), ColDim, 13); return; }
        int nextIdx = visited % items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            bool isNext    = i == nextIdx;
            bool isPast    = visited > 0 && (i < nextIdx || (nextIdx == 0 && visited >= items.Count));
            string prefix  = isNext ? "▶ " : isPast ? "✓ " : "  ";
            Color c        = isNext ? activeColor : isPast ? ColVisited : new Color(0.65f, 0.65f, 0.65f);
            AddLine($"  {prefix}{i + 1}. {items[i]}", c, 13);
        }
    }

    // ── 反射工具 ─────────────────────────────────────────────

    private static RoomSet? GetRoomSet(ActModel act)
    {
        try { return RoomsField?.GetValue(act) as RoomSet; }
        catch { return null; }
    }

    private static string? SafeGet(Func<string> f)
    {
        try { return f(); } catch { return null; }
    }

    // ── UI 辅助 ──────────────────────────────────────────────

    private void AddSectionHeader(string text)
    {
        if (_content == null) return;
        var pad = new MarginContainer();
        pad.AddThemeConstantOverride("margin_top", 8);
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color", ColTitle);
        lbl.AddThemeFontSizeOverride("font_size", FS(13));
        pad.AddChild(lbl);
        _content.AddChild(pad);
    }

    private void AddKeyValue(string key, string? value, Color valueColor)
    {
        if (_content == null) return;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var k = new Label();
        k.Text = key + ":";
        k.AddThemeColorOverride("font_color", ColDim);
        k.AddThemeFontSizeOverride("font_size", FS(12));
        k.CustomMinimumSize = new Vector2(110, 0);
        row.AddChild(k);

        var v = new Label();
        v.Text = value ?? STS2AdvisorI18n.Pick("(none)", "（无）");
        v.AddThemeColorOverride("font_color", string.IsNullOrEmpty(value) ? ColDim : valueColor);
        v.AddThemeFontSizeOverride("font_size", FS(12));
        v.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        v.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(v);
        _content.AddChild(row);
    }

    private void AddLine(string text, Color color, int fontSize = 12)
    {
        if (_content == null) return;
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", FS(fontSize));
        lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _content.AddChild(lbl);
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
    }

    private static string LocalizePointType(MapPointType t) => t switch
    {
        MapPointType.Monster    => STS2AdvisorI18n.Pick("Normal fight", "普通战斗"),
        MapPointType.Elite      => STS2AdvisorI18n.Pick("Elite fight", "精英战斗"),
        MapPointType.Boss       => STS2AdvisorI18n.Pick("Boss", "首领"),
        MapPointType.RestSite   => STS2AdvisorI18n.Pick("Rest site", "休息地"),
        MapPointType.Shop       => STS2AdvisorI18n.Pick("Shop", "商店"),
        MapPointType.Treasure   => STS2AdvisorI18n.Pick("Chest", "宝箱"),
        MapPointType.Unknown    => STS2AdvisorI18n.Pick("Unknown node", "未知节点"),
        MapPointType.Ancient    => STS2AdvisorI18n.Pick("Start/Ancient", "起始/古代"),
        _                       => t.ToString()
    };

    private static string LocalizeRoomType(RoomType rt) => rt switch
    {
        RoomType.Monster  => STS2AdvisorI18n.Pick("Normal fight", "普通战斗"),
        RoomType.Elite    => STS2AdvisorI18n.Pick("Elite fight", "精英战斗"),
        RoomType.Shop     => STS2AdvisorI18n.Pick("Shop", "商店"),
        RoomType.Treasure => STS2AdvisorI18n.Pick("Chest", "宝箱"),
        RoomType.Event    => STS2AdvisorI18n.Pick("Event", "事件"),
        _                 => rt.ToString()
    };

    // ── UI 构建 ──────────────────────────────────────────────

    private void BuildUi()
    {
        _root = new PanelContainer();
        _root.Position = new Vector2(10, 60);
        _root.CustomMinimumSize = new Vector2((int)(460 * _scale), 0);
        _root.AddThemeStyleboxOverride("panel", MakeRootStyle());
        AddChild(_root);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 0);
        _root.AddChild(outer);

        // 标题栏
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", MakeHeaderStyle());
        outer.AddChild(header);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        header.AddChild(hbox);

        _titleLabel = new Label();
        _titleLabel.Text = STS2AdvisorI18n.Pick("All-Seeing Star", "全知全能之星");
        _titleLabel.AddThemeColorOverride("font_color", ColTitle);
        _titleLabel.AddThemeFontSizeOverride("font_size", FS(14));
        _titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _titleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hbox.AddChild(_titleLabel);

        foreach (var (txt, action) in new (string, Action)[]
        {
            ("↺", Refresh),
            ("-", () => ChangeScale(-0.15f)),
            ("+", () => ChangeScale(0.15f)),
            ("×", () => { if (_root != null) _root.Visible = false; })
        })
        {
            var btn = new Button();
            btn.Text = txt;
            btn.Flat = true;
            btn.CustomMinimumSize = new Vector2(24, 24);
            btn.AddThemeColorOverride("font_color", ColDim);
            btn.AddThemeFontSizeOverride("font_size", txt == "×" ? FS(15) : FS(13));
            btn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            btn.Pressed += action;
            hbox.AddChild(btn);
        }

        var sep = new Panel();
        sep.CustomMinimumSize = new Vector2(0, 1);
        sep.AddThemeStyleboxOverride("panel", MakeSepStyle());
        outer.AddChild(sep);

        _statusLabel = new Label();
        string hk = HotkeyConfig.GetTokenText(HotkeyAction.ShaNagbaImuruToggle);
        _statusLabel.Text = STS2AdvisorI18n.Pick($"{hk} Toggle  |  ↺ Refresh", $"{hk} 开关  |  ↺ 刷新");
        _statusLabel.AddThemeColorOverride("font_color", ColDim);
        _statusLabel.AddThemeFontSizeOverride("font_size", FS(11));
        var statusPad = new MarginContainer();
        statusPad.AddThemeConstantOverride("margin_left",   8);
        statusPad.AddThemeConstantOverride("margin_top",    2);
        statusPad.AddThemeConstantOverride("margin_bottom", 2);
        statusPad.AddChild(_statusLabel);
        outer.AddChild(statusPad);

        _scroll = new ScrollContainer();
        _scroll.CustomMinimumSize    = new Vector2((int)(460 * _scale), (int)(680 * _scale));
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        outer.AddChild(_scroll);

        var pad = new MarginContainer();
        pad.AddThemeConstantOverride("margin_left",   8);
        pad.AddThemeConstantOverride("margin_right",  8);
        pad.AddThemeConstantOverride("margin_top",    6);
        pad.AddThemeConstantOverride("margin_bottom", 8);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 3);
        pad.AddChild(_content);
        _scroll.AddChild(pad);
    }

    private void ChangeScale(float delta)
    {
        _scale = Mathf.Clamp(_scale + delta, MinScale, MaxScale);
        // 不缩放节点（会导致模糊），而是重建 UI 以更新字号和尺寸
        RebuildUiShell();
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state != null) RebuildContent(state);
    }

    // 重建面板外壳（标题栏、滚动区尺寸等）
    private void RebuildUiShell()
    {
        if (_root != null)
        {
            var pos = _root.Position;
            _root.QueueFree();
            _root = null;
            BuildUi();
            if (_root != null) _root.Position = pos;
        }
    }

    // ── 输入 ─────────────────────────────────────────────────

    public override void _Input(InputEvent evt)
    {
        if (evt is InputEventKey key &&
            key.Pressed &&
            key.Keycode == HotkeyConfig.GetKey(HotkeyAction.ShaNagbaImuruToggle))
        {
            if (_root == null) return;
            if (!_root.Visible) Refresh();
            else _root.Visible = false;
        }

        if (_root == null || !_root.Visible) return;

        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            var local  = _root.GetLocalMousePosition();
            bool inHdr = local.Y < 44 && local.X >= 0 && local.X <= _root.Size.X;
            if (mb.Pressed && inHdr)
            {
                if (!PanelDragState.TryStart(nameof(Sha_Nagba_Imuru))) return;
                _dragging = true;
                _dragOffset = _root.Position - GetViewport().GetMousePosition();
            }
            else if (!mb.Pressed && _dragging)
            {
                _dragging = false;
                PanelDragState.End(nameof(Sha_Nagba_Imuru));
            }
        }
        if (evt is InputEventMouseMotion && _dragging)
            _root.Position = GetViewport().GetMousePosition() + _dragOffset;
    }

    // ── StyleBox ─────────────────────────────────────────────

    private static StyleBoxFlat MakeRootStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = ColBg; s.SetCornerRadiusAll(10);
        s.BorderColor = ColBorder; s.SetBorderWidthAll(1);
        s.ShadowColor = new Color(0, 0, 0, 0.5f);
        s.ShadowSize = 12; s.ShadowOffset = new Vector2(0, 3);
        return s;
    }

    private static StyleBoxFlat MakeHeaderStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(1, 1, 1, 0.04f);
        s.CornerRadiusTopLeft = s.CornerRadiusTopRight = 10;
        s.ContentMarginLeft = 12; s.ContentMarginRight = 6;
        s.ContentMarginTop = s.ContentMarginBottom = 8;
        return s;
    }

    private static StyleBoxFlat MakeSepStyle()
    {
        var s = new StyleBoxFlat();
        s.BgColor = new Color(1, 1, 1, 0.07f);
        return s;
    }

    private static StyleBoxFlat MakeCardStyle(Color bg)
    {
        var s = new StyleBoxFlat();
        s.BgColor = bg; s.SetCornerRadiusAll(4);
        s.ContentMarginLeft = s.ContentMarginRight = 7;
        s.ContentMarginTop = s.ContentMarginBottom = 3;
        s.BorderColor = new Color(1, 1, 1, 0.05f);
        s.SetBorderWidthAll(1);
        return s;
    }
}