using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Events;
namespace STS2Advisor.Scripts;

// ============================================================
//  通用变形结果预测工具
//  可被任何 IEventPredictor 调用
// ============================================================

public static class TransformPredictor
{
    /// <summary>
    /// 还原 GetDefaultTransformationOptions + GetFilteredTransformationOptions。
    /// 返回过滤后的候选卡池数组，调用方用 rng.NextInt(0, pool.Length) 取索引。
    /// </summary>
    public static CardModel[] GetFilteredPool(CardModel original, bool isInCombat)
    {
        // 卡池来源：Quest/Event/Ancient/Token 用无色池，其余用角色自身池
        CardPoolModel poolModel =
            (original.Type == CardType.Quest
             || original.Rarity == CardRarity.Event
             || original.Rarity == CardRarity.Ancient
             || original.Rarity == CardRarity.Token)
            ? ModelDb.CardPool<ColorlessCardPool>()
            : (original.Pool ?? ModelDb.CardPool<ColorlessCardPool>());

        if (original.Owner == null || original.RunState == null)
            return Array.Empty<CardModel>();

        var unlocked = poolModel.GetUnlockedCards(
            original.Owner.UnlockState,
            original.RunState.CardMultiplayerConstraint);

        IEnumerable<CardModel> source = unlocked;

        // 稀有度过滤：非 Special/Event 稀有度只保留 Common/Uncommon/Rare
        // 原码：if ((uint)(rarity - 8) > 1u) → 即不是 Special(8) 或 Event(9)
        bool isSpecialRarity = (uint)((int)original.Rarity - 8) <= 1u;
        if (!isSpecialRarity)
        {
            source = source.Where(c =>
            {
                // (uint)(rarity - 2) <= 2u → Common(2), Uncommon(3), Rare(4)
                return (uint)((int)c.Rarity - 2) <= 2u;
            });
        }

        // 战斗中额外过滤
        if (isInCombat)
            source = source.Where(c => c.CanBeGeneratedInCombat);

        // 排除原卡自身
        source = source.Where(c => c.Id != original.Id);

        return source.ToArray();
    }

    /// <summary>
    /// 用 seed + counter 重建一个临时 Rng 来 peek 变形结果，
    /// 同时推进 mirrorRng 一次保持 Counter 同步。
    /// </summary>
    public static string PeekTransform(CardModel original, Rng mirrorRng, bool isInCombat)
    {
        var pool = GetFilteredPool(original, isInCombat);
        if (pool.Length == 0) return EventPredictionText.NoTransformTargets();

        // 用相同 seed + 当前 counter 重建，保证与 mirrorRng 读取完全一致
        var peekRng = new Rng(mirrorRng.Seed, mirrorRng.Counter);
        int index   = peekRng.NextInt(0, pool.Length);

        // mirrorRng 本身也推进一次，保持后续 Counter 正确
        mirrorRng.NextInt(0, pool.Length);

        return pool[index].Id.Entry;
    }
}

// ============================================================
//  EndlessConveyor 预测器（含 JellyLiver 变形预测）
// ============================================================

public class EndlessConveyorPredictor : IEventPredictor
{
    public Type EventType => typeof(EndlessConveyor);

    private const int PredictSteps = 10;

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var player = eventModel.Owner;
        if (player == null) return new();

        bool hasHeal = player.Creature.CurrentHp < player.Creature.MaxHp;
        bool hasPot  = player.HasOpenPotionSlots;

        var results   = new List<EventPrediction>();
        string lastId = "";
        int numGrabs  = 0;

        // 初始展示（CalculateVars 中的第一次 RollDish，消耗一次 NextFloat）
        numGrabs++;
        var init = RollDish(mirrorRng, numGrabs, ref lastId, hasHeal, hasPot);
        results.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Initial dish", "初始展示"),
            FormatDish(init),
            ToTag(init.Id)));

        // 后续抓取
        for (int i = 1; i <= PredictSteps; i++)
        {
            numGrabs++;
            var dish = RollDish(mirrorRng, numGrabs, ref lastId, hasHeal, hasPot);

            string label = dish.Forced
                ? STS2AdvisorI18n.Pick($"Pull {i} [Forced]", $"第{i}次 [必出]")
                : STS2AdvisorI18n.Pick($"Pull {i} ({dish.ChancePct:F0}%)", $"第{i}次 ({dish.ChancePct:F0}%)");

            string value = FormatDish(dish);
            if (dish.IsFree) value += STS2AdvisorI18n.Pick(" [Free]", " [免费]");

            results.Add(new EventPrediction(label, value, ToTag(dish.Id)));

            // JELLY_LIVER：在 RollDish 之后 mirrorRng 的 Counter 位置
            // 与游戏调用 CardCmd.TransformToRandom(card, base.Rng) 时一致。
            // 下一次 NextInt 就是 CardFactory 里 rng.NextItem(filteredPool)。
            if (dish.Id == "JELLY_LIVER")
                results.AddRange(PredictJellyLiver(player, mirrorRng));
        }

        return results;
    }

    // ── JellyLiver 变形预测 ──────────────────────────────────

    private static List<EventPrediction> PredictJellyLiver(
        MegaCrit.Sts2.Core.Entities.Players.Player player, Rng mirrorRng)
    {
        var transformable = player.Deck.Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (transformable.Count == 0)
            return new()
            {
                new(
                    STS2AdvisorI18n.Pick("  └ Transformation", "  └ 变形结果"),
                    EventPredictionText.NoTransformableCards(),
                    PredictionTag.Normal)
            };

        // 按卡池分组，不同池的候选集不同，结果不同
        var byPool = transformable.GroupBy(c =>
        {
            bool isSpecial = c.Type == CardType.Quest
                || c.Rarity == CardRarity.Event
                || c.Rarity == CardRarity.Ancient
                || c.Rarity == CardRarity.Token;
            return isSpecial ? "colorless" : (c.Pool?.Id.Entry ?? "colorless");
        }).ToList();

        // 记录当前 Counter 位置，对每个池都用 seed+counter 重建独立读取
        int currentCounter = mirrorRng.Counter;
        uint seed          = mirrorRng.Seed;

        if (byPool.Count == 1)
        {
            // 单池：所有牌变形结果相同，无论选哪张
            var pool = TransformPredictor.GetFilteredPool(byPool[0].First(), isInCombat: false);
            if (pool.Length == 0)
                return new()
                {
                    new(
                        STS2AdvisorI18n.Pick("  └ Transformation", "  └ 变形结果"),
                        EventPredictionText.NoTransformTargets(),
                        PredictionTag.Normal)
                };

            var peekRng = new Rng(seed, currentCounter);
            int index   = peekRng.NextInt(0, pool.Length);
            string result = LocText.Of(pool[index]);

            // mirrorRng 推进一次保持同步
            mirrorRng.NextInt(0, pool.Length);

            return new()
            {
                new(
                    STS2AdvisorI18n.Pick("  └ Transformation", "  └ 变形结果"),
                    result,
                    PredictionTag.Warning)
            };
        }
        else
        {
            // 多池：每个池用相同 Counter 位置独立 peek，结果各自正确
            var rows = new List<EventPrediction>
            {
                new(
                    STS2AdvisorI18n.Pick("  └ Transformation", "  └ 变形结果"),
                    STS2AdvisorI18n.Pick("Depends on selected card:", "取决于选哪张牌："),
                    PredictionTag.Normal)
            };

            foreach (var group in byPool)
            {
                var pool = TransformPredictor.GetFilteredPool(group.First(), isInCombat: false);
                if (pool.Length == 0) continue;

                // 每个池都从相同的 Counter 位置重建读取，互不干扰
                var peekRng = new Rng(seed, currentCounter);
                int index   = peekRng.NextInt(0, pool.Length);
                string result = LocText.Of(pool[index]);

                string poolName = group.Key == "colorless"
                    ? STS2AdvisorI18n.Pick("    Pick Colorless/Special", "    选无色/特殊牌")
                    : STS2AdvisorI18n.Pick(
                        $"    Pick {group.Key.Replace("_CARD_POOL", "").Replace("_", " ")} card",
                        $"    选{group.Key.Replace("_CARD_POOL", "").Replace("_", "")}角色牌");

                rows.Add(new(poolName, result, PredictionTag.Warning));
            }

            // mirrorRng 推进一次保持后续 Counter 同步
            mirrorRng.NextInt(0, 2);

            return rows;
        }
    }

    // ── RollDish 模拟 ────────────────────────────────────────

    private record struct DishRoll(string Id, bool Forced, bool IsFree, float ChancePct);

    private static DishRoll RollDish(
        Rng mirrorRng, int numOfGrabs, ref string lastDishId,
        bool hasHeal, bool hasPot)
    {
        if (numOfGrabs % 5 == 0)
        {
            lastDishId = "SEAPUNK_SALAD";
            return new DishRoll("SEAPUNK_SALAD", true, false, 100f);
        }

        var pool = new List<(string id, float w)>
        {
            ("CAVIAR",       6f), ("SPICY_SNAPPY", 3f),
            ("JELLY_LIVER",  3f), ("FRIED_EEL",    3f),
        };
        if (hasPot)         pool.Add(("SUSPICIOUS_CONDIMENT", 3f));
        if (hasHeal)        pool.Add(("CLAM_ROLL",            6f));
        if (numOfGrabs > 1) pool.Add(("GOLDEN_FYSH",          1f));

        string currentLast = lastDishId;
        pool = pool.Where(d => d.id != currentLast).ToList();

        float total = pool.Sum(d => d.w);
        float roll  = mirrorRng.NextFloat() * total;
        float acc   = 0f;

        foreach (var (id, w) in pool)
        {
            acc += w;
            if (roll < acc)
            {
                lastDishId = id;
                return new DishRoll(id, false,
                    id == "GOLDEN_FYSH",
                    total > 0f ? w / total * 100f : 0f);
            }
        }

        var last = pool[^1];
        lastDishId = last.id;
        return new DishRoll(last.id, false, last.id == "GOLDEN_FYSH", 0f);
    }

    // ── 显示辅助 ─────────────────────────────────────────────

    private static string FormatDish(DishRoll d) => d.Id switch
    {
        "CAVIAR"               => STS2AdvisorI18n.Pick("Caviar  +4 Max HP", "鱼子酱  +4 最大生命"),
        "CLAM_ROLL"            => STS2AdvisorI18n.Pick("Clam Roll  Heal 10", "蛤蜊卷  回复 10 生命"),
        "SPICY_SNAPPY"         => STS2AdvisorI18n.Pick("Spicy Snappy  Upgrade card", "麻辣螃蟹  升级卡牌"),
        "JELLY_LIVER"          => STS2AdvisorI18n.Pick("Jelly Liver  Transform card ->", "果冻肝  变形卡牌 ->"),
        "FRIED_EEL"            => STS2AdvisorI18n.Pick("Fried Eel  Colorless card", "炸鳗鱼  无色卡牌"),
        "SUSPICIOUS_CONDIMENT" => STS2AdvisorI18n.Pick("Suspicious Condiment  Gain potion", "可疑调料  获得药水"),
        "GOLDEN_FYSH"          => STS2AdvisorI18n.Pick("Golden Fysh  +75 Gold", "黄金鱼  +75 金币"),
        "SEAPUNK_SALAD"        => STS2AdvisorI18n.Pick("Seapunk Salad  Feeding Frenzy", "海朋克沙拉  疯狂进食"),
        _                      => d.Id
    };

    private static PredictionTag ToTag(string id) => id switch
    {
        "CAVIAR" or "GOLDEN_FYSH" or "SEAPUNK_SALAD" or "CLAM_ROLL" => PredictionTag.Good,
        "JELLY_LIVER"                                                 => PredictionTag.Warning,
        _                                                             => PredictionTag.Normal
    };
}