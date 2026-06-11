using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace STS2Advisor.Scripts;

public sealed class AdvisorRuntimeCore
{
    public sealed class PlayerSnapshot
    {
        public required ulong NetId;
        public required string Label;
        public required IReadOnlyList<CardModel> DrawPile;
        public required IReadOnlyList<CardModel> DiscardPile;
        public required IReadOnlyList<CardModel>? PredictedMergedShuffle;
        public required bool Changed;
    }

    public sealed class PanelSnapshot
    {
        public required int TotalPlayers;
        public required List<PlayerSnapshot> Players;
    }

    private const double RefreshIntervalSeconds = 0.25;

    private double _refreshAccum;
    private readonly Dictionary<ulong, string> _lastPileSignatureByPlayer = new();

    // 多人模式：反射读取 RewardSynchronizer._localPlayerId 的静态缓存
    private static FieldInfo? _cachedLocalPlayerIdField;///?代表可能为空，_是一个职业通用写法

    public void ResetCache()
    {
        _refreshAccum = 0;
        _lastPileSignatureByPlayer.Clear();
    }

    public bool TryBuildSnapshot(bool force, double delta, out PanelSnapshot snapshot)
    {
        snapshot = null!;

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null)
            return false;

        if (!force)
        {
            _refreshAccum += delta;
            if (_refreshAccum < RefreshIntervalSeconds)
                return false;
        }
        _refreshAccum = 0;

        var orderedPlayers = GetPlayersLocalFirst(state);///是我自定义的一个方法，根据本地玩家优先级排序
        int showCount = Math.Min(4, orderedPlayers.Count);
        var players = new List<PlayerSnapshot>(showCount);

        for (int i = 0; i < showCount; i++)
        {
            var player = orderedPlayers[i];
            var drawPile = player.PlayerCombatState?.DrawPile.Cards;
            var discardPile = PileType.Discard.GetPile(player).Cards;
            if (drawPile == null || discardPile == null)
                continue;

            var shuffle = player.RunState?.Rng.Shuffle;
            var sig = BuildPileSignature(drawPile, discardPile, shuffle);
            bool changed = force
                || !_lastPileSignatureByPlayer.TryGetValue(player.NetId, out var oldSig)
                || oldSig != sig;

            _lastPileSignatureByPlayer[player.NetId] = sig;

            IReadOnlyList<CardModel>? predicted = null;
            if (shuffle != null && drawPile.Count + discardPile.Count > 0)
                predicted = SimulateMergedReshuffle(drawPile, discardPile, shuffle);

            players.Add(new PlayerSnapshot
            {
                NetId = player.NetId,
                Label = BuildPlayerLabel(player),
                DrawPile = drawPile,
                DiscardPile = discardPile,
                PredictedMergedShuffle = predicted,
                Changed = changed
            });
        }

        snapshot = new PanelSnapshot
        {
            TotalPlayers = orderedPlayers.Count,
            Players = players
        };
        return true;
    }

    private static string BuildPlayerLabel(Player p)
    {
        try
        {
            // 尽量给一个稳定可辨识的标签（不同版本字段可能不一样，安全降级到 NetId）
            var charId = p.Character?.Id.Entry;
            if (!string.IsNullOrWhiteSpace(charId))
                return $"{charId}#{p.NetId}";
        }
        catch { }
        return $"Net#{p.NetId}";
    }

    private static List<Player> GetPlayersLocalFirst(CombatState state)
    {
        var players = state.Players.ToList();///静态成员：用类名点
                                             //实例成员：用对象点
        if (players.Count <= 1)
            return players;

        ulong? localId = null;
        try
        {
            var runMgr = MegaCrit.Sts2.Core.Runs.RunManager.Instance;
            if (runMgr != null && runMgr.RewardSynchronizer != null)
            {
                if (_cachedLocalPlayerIdField == null)
                {
                    _cachedLocalPlayerIdField = typeof(MegaCrit.Sts2.Core.Multiplayer.Game.RewardSynchronizer)
                        .GetField("_localPlayerId", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_cachedLocalPlayerIdField != null)
                {
                    var obj = _cachedLocalPlayerIdField.GetValue(runMgr.RewardSynchronizer);
                    if (obj is ulong id) localId = id;
                }
            }
        }
        catch { }

        if (localId == null)
            return players;

        return players
            .OrderByDescending(p => p.NetId == localId.Value)
            .ThenBy(p => p.NetId)
            .ToList();
    }

    private static string BuildPileSignature(
        IReadOnlyList<CardModel> draw,
        IReadOnlyList<CardModel> discard,
        Rng? shuffleRng)
    {
        var sb = new StringBuilder();
        sb.Append('D').Append(draw.Count);
        foreach (var c in draw)
            sb.Append('|').Append(RuntimeHelpers.GetHashCode(c));
        sb.Append('^');
        sb.Append('d').Append(discard.Count);
        foreach (var c in discard)
            sb.Append('|').Append(RuntimeHelpers.GetHashCode(c));
        if (shuffleRng != null)
            sb.Append("^S").Append(shuffleRng.Seed).Append(',').Append(shuffleRng.Counter);
        return sb.ToString();
    }

    /// <summary>
    /// 对齐 <c>CardPileCmd.Shuffle</c>：discard.ToList()，再 foreach draw 的 HashSet 并入，然后 StableShuffle（Sort + UnstableShuffle）。
    /// 不模拟 Hook.ModifyShuffleOrder / Debug 强制顶牌。
    /// </summary>
    private static List<CardModel> SimulateMergedReshuffle(
        IReadOnlyList<CardModel> drawPile,
        IReadOnlyList<CardModel> discardPile,
        Rng gameShuffleRng)
    {
        var list = new List<CardModel>(discardPile.Count + drawPile.Count);
        foreach (var c in discardPile)
            list.Add(c);
        var drawSet = new HashSet<CardModel>();
        foreach (var c in drawPile)
            drawSet.Add(c);
        foreach (var c in drawSet)
            list.Add(c);

        var peek = new Rng(gameShuffleRng.Seed, gameShuffleRng.Counter);
        StableShuffleInPlace(list, peek);
        return list;
    }

    /// <summary>与 <c>ListExtensions.StableShuffle</c> 一致：复制排序写回 + Fisher–Yates。</summary>
    private static void StableShuffleInPlace(List<CardModel> list, Rng rng)
    {
        var sorted = list.ToList();
        sorted.Sort();
        for (int i = 0; i < list.Count; i++)
            list[i] = sorted[i];
        UnstableShuffleInPlace(list, rng);
    }

    /// <summary>与游戏 <c>ListExtensions.UnstableShuffle</c> 一致。</summary>
    private static void UnstableShuffleInPlace(List<CardModel> list, Rng rng)
    {
        int num = list.Count;
        while (num > 1)
        {
            num--;
            int j = rng.NextInt(num + 1);
            (list[j], list[num]) = (list[num], list[j]);
        }
    }
}
