using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

// ============================================================
//  通用变形事件预测器
//  适用于所有调用 CardCmd.TransformToRandom(card, base.Rng) 的事件
// ============================================================

public class GenericTransformPredictor : IEventPredictor
{
    private readonly Type   _eventType;
    private readonly int    _transformCount;
    private readonly string _displayName;

    public Type EventType => _eventType;

    public GenericTransformPredictor(Type eventType, string displayName, int transformCount = 1)
    {
        _eventType      = eventType;
        _displayName    = displayName;
        _transformCount = transformCount;
    }

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var player = eventModel.Owner;
        if (player == null) return new();

        var results = new List<EventPrediction>();

        var transformable = player.Deck.Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (transformable.Count == 0)
        {
            results.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Transformation", "变形结果"),
                EventPredictionText.NoTransformableCards(),
                PredictionTag.Normal));
            return results;
        }

        results.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Event", "事件"),
            _displayName,
            PredictionTag.Normal));

        var byPool = transformable.GroupBy(GetPoolKey).ToList();

        if (byPool.Count == 1)
        {
            // 单池：结果与选哪张牌无关
            results.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Card choice does not change outcome", "选牌不影响结果"),
                STS2AdvisorI18n.Pick(
                    $"{transformable.Count} transformable card(s).",
                    $"共 {transformable.Count} 张可变形卡牌。"),
                PredictionTag.Normal));

            for (int i = 0; i < _transformCount; i++)
            {
                string label = _transformCount > 1
                    ? STS2AdvisorI18n.Pick($"Transform #{i + 1}", $"变形第 {i + 1} 张")
                    : STS2AdvisorI18n.Pick("Transformation", "变形结果");
                string result = PeekForCard(transformable[0], mirrorRng);
                results.Add(new EventPrediction(label, result, PredictionTag.Warning));
            }
        }
        else
        {
            // 多池：对每个池独立 peek，使用相同的 Counter 位置
            // 关键修复：用 mirrorRng.Seed + mirrorRng.Counter 重建一个临时 Rng，
            // 对每个池单独调用 NextInt(0, pool.Length)，互不干扰
            results.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Outcome depends on selected card", "结果取决于选牌"),
                STS2AdvisorI18n.Pick(
                    $"{byPool.Count} different card pool(s).",
                    $"共 {byPool.Count} 种卡池。"),
                PredictionTag.Normal));

            int currentCounter = mirrorRng.Counter;
            uint seed          = mirrorRng.Seed;

            foreach (var group in byPool)
            {
                var rep  = group.First();
                var pool = TransformPredictor.GetFilteredPool(rep, isInCombat: false);
                if (pool.Length == 0) continue;

                // 每个池都从相同的 Counter 位置独立读取一次 NextInt
                var peekRng = new Rng(seed, currentCounter);
                int index   = peekRng.NextInt(0, pool.Length);
                string result = LocText.Of(pool[index]);

                string poolLabel = group.Key == "colorless"
                    ? STS2AdvisorI18n.Pick("Pick Colorless/Special card", "选无色/特殊牌")
                    : STS2AdvisorI18n.Pick(
                        $"Pick {GroupDisplayName(group.Key)} card",
                        $"选{GroupDisplayName(group.Key)}牌");

                results.Add(new EventPrediction(poolLabel, result, PredictionTag.Warning));
            }

            // mirrorRng 本身推进一次，保持后续 Counter 正确
            mirrorRng.NextInt(0, 2);
        }

        return results;
    }

    // ── 辅助 ─────────────────────────────────────────────────

    /// <summary>
    /// 用相同 seed+counter 重建 Rng 来 peek，不影响 mirrorRng 的 Counter。
    /// 用于单池情况下只需要读取一次结果。
    /// </summary>
    private static string PeekForCard(CardModel card, Rng mirrorRng)
    {
        var pool = TransformPredictor.GetFilteredPool(card, isInCombat: false);
        if (pool.Length == 0) return EventPredictionText.NoTransformTargets();

        // 用当前 seed + counter 重建，peek 后不改变 mirrorRng
        var peekRng = new Rng(mirrorRng.Seed, mirrorRng.Counter);
        int index   = peekRng.NextInt(0, pool.Length);

        // mirrorRng 本身也要推进，保持后续 Counter 同步
        mirrorRng.NextInt(0, pool.Length);

        return LocText.Of(pool[index]);
    }

    private static string GetPoolKey(CardModel c)
    {
        bool isSpecial = c.Type == CardType.Quest
            || c.Rarity == CardRarity.Event
            || c.Rarity == CardRarity.Ancient
            || c.Rarity == CardRarity.Token;
        return isSpecial ? "colorless" : (c.Pool?.Id.Entry ?? "colorless");
    }

    private static string GroupDisplayName(string poolKey)
    {
        // SILENT_CARD_POOL → Silent，IRONCLAD_CARD_POOL → Ironclad 等
        return poolKey
            .Replace("_CARD_POOL", "")
            .Replace("_", " ");
    }
}