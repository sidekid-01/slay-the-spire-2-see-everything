using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class NeowPredictor : IEventPredictor
{
    public Type EventType => typeof(Neow);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var neow = eventModel as Neow;
        var owner = eventModel.Owner;
        if (neow == null || owner == null)
            return new();

        if (owner.RunState.Modifiers.Count > 0)
            return PredictModifierFlow(owner.RunState.Modifiers);

        return PredictStandardFlow(owner, mirrorRng);
    }

    private static List<EventPrediction> PredictModifierFlow(IEnumerable<ModifierModel> modifiers)
    {
        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Mode", "模式"),
                STS2AdvisorI18n.Pick("Modifier-driven Neow options", "词缀驱动的 Neow 选项"),
                PredictionTag.Normal)
        };

        int i = 1;
        foreach (var modifier in modifiers)
        {
            string title = LocText.Of(modifier.NeowOptionTitle);
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Option {i}", $"选项 {i}"),
                string.IsNullOrWhiteSpace(title) ? modifier.Id.Entry : title,
                PredictionTag.Warning));
            i++;
        }

        return rows;
    }

    private static List<EventPrediction> PredictStandardFlow(MegaCrit.Sts2.Core.Entities.Players.Player owner, Rng mirrorRng)
    {
        var cursePool = BuildCursePool(owner);
        int curseIndex = mirrorRng.NextInt(0, cursePool.Count);
        var cursed = cursePool[curseIndex];

        var positivePool = BuildPositivePool(owner, cursed, mirrorRng);

        // list2.ToList().UnstableShuffle(base.Rng).Take(2)
        var shuffled = UnstableShuffle(positivePool.ToList(), mirrorRng);
        var selectedPositive = shuffled.Take(2).ToList();

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Cursed choice", "诅咒选项"),
                RelicName(cursed),
                PredictionTag.Bad)
        };

        for (int i = 0; i < selectedPositive.Count; i++)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Positive option {i + 1}", $"正向选项 {i + 1}"),
                DescribeRelicOption(selectedPositive[i], owner),
                PredictionTag.Good));

            if (selectedPositive[i] is NewLeaf)
                rows.AddRange(PredictNewLeafTransform(owner));
            if (selectedPositive[i] is ArcaneScroll)
                rows.AddRange(PredictArcaneScroll(owner));
            if (selectedPositive[i] is MassiveScroll)
                rows.AddRange(PredictMassiveScroll(owner));
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Display order", "显示顺序"),
            STS2AdvisorI18n.Pick(
                "In game: two positive options are shown first, then cursed option.",
                "游戏中显示顺序：先两个正向选项，再诅咒选项。"),
            PredictionTag.Normal));

        return rows;
    }

    private static List<RelicModel> BuildCursePool(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var list = new List<RelicModel>
        {
            ModelDb.Relic<CursedPearl>(),
            ModelDb.Relic<LargeCapsule>(),
            ModelDb.Relic<LeafyPoultice>(),
            ModelDb.Relic<PrecariousShears>()
        };

        if (ScrollBoxes.CanGenerateBundles(owner))
            list.Add(ModelDb.Relic<ScrollBoxes>());
        if (owner.RunState.Players.Count == 1)
            list.Add(ModelDb.Relic<SilverCrucible>());

        return list;
    }

    private static List<RelicModel> BuildPositivePool(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        RelicModel cursed,
        Rng mirrorRng)
    {
        var list = new List<RelicModel>
        {
            ModelDb.Relic<ArcaneScroll>(),
            ModelDb.Relic<BoomingConch>(),
            ModelDb.Relic<Pomander>(),
            ModelDb.Relic<GoldenPearl>(),
            ModelDb.Relic<LeadPaperweight>(),
            ModelDb.Relic<NewLeaf>(),
            ModelDb.Relic<NeowsTorment>(),
            ModelDb.Relic<PreciseScissors>(),
            ModelDb.Relic<LostCoffer>()
        };

        if (cursed is CursedPearl)
            list.RemoveAll(r => r is GoldenPearl);
        if (cursed is PrecariousShears)
            list.RemoveAll(r => r is PreciseScissors);
        if (cursed is LeafyPoultice)
            list.RemoveAll(r => r is NewLeaf);

        if (owner.RunState.Players.Count > 1)
            list.Add(ModelDb.Relic<MassiveScroll>());

        if (mirrorRng.NextInt(0, 2) == 0)
            list.Add(ModelDb.Relic<NutritiousOyster>());
        else
            list.Add(ModelDb.Relic<StoneHumidifier>());

        if (cursed is not LargeCapsule)
        {
            if (mirrorRng.NextInt(0, 2) == 0)
                list.Add(ModelDb.Relic<LavaRock>());
            else
                list.Add(ModelDb.Relic<SmallCapsule>());
        }

        return list;
    }

    private static List<RelicModel> UnstableShuffle(List<RelicModel> list, Rng rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int j = rng.NextInt(n + 1);
            (list[j], list[n]) = (list[n], list[j]);
        }
        return list;
    }

    private static string RelicName(RelicModel relic) => LocText.Of(relic);

    private static string DescribeRelicOption(RelicModel relic, MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        string name = RelicName(relic);
        return relic switch
        {
            NewLeaf => name + STS2AdvisorI18n.Pick(" (Transforms 1 card)", "（变形 1 张牌）"),
            ArcaneScroll => name + STS2AdvisorI18n.Pick(" (Gain 1 Rare card)", "（获得 1 张稀有牌）"),
            MassiveScroll => name + STS2AdvisorI18n.Pick(" (Offer 3 cards, choose 1)", "（提供3张牌，选择1张）"),
            _ => name
        };
    }

    private static IEnumerable<EventPrediction> PredictNewLeafTransform(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var transformable = owner.Deck.Cards.Where(c => c.IsTransformable).ToList();
        if (transformable.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ New Leaf", "  └ 新叶"),
                EventPredictionText.NoTransformableCards(),
                PredictionTag.Normal);
            yield break;
        }

        var byPool = transformable.GroupBy(GetPoolKey).ToList();
        var niche = owner.RunState.Rng.Niche;
        int counter = niche.Counter;
        uint seed = niche.Seed;

        if (byPool.Count == 1)
        {
            var rep = byPool[0].First();
            var pool = TransformPredictor.GetFilteredPool(rep, isInCombat: false);
            if (pool.Length == 0)
            {
                yield return new EventPrediction(
                    STS2AdvisorI18n.Pick("  └ Transform", "  └ 变形"),
                    EventPredictionText.NoTransformTargets(),
                    PredictionTag.Normal);
                yield break;
            }

            var peekRng = new Rng(seed, counter);
            int idx = peekRng.NextInt(0, pool.Length);
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Transform result", "  └ 变形结果"),
                LocText.Of(pool[idx]),
                PredictionTag.Warning);
            yield break;
        }

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Transform result", "  └ 变形结果"),
            STS2AdvisorI18n.Pick("Depends on selected card:", "取决于选择哪张牌："),
            PredictionTag.Normal);

        foreach (var group in byPool)
        {
            var rep = group.First();
            var pool = TransformPredictor.GetFilteredPool(rep, isInCombat: false);
            if (pool.Length == 0) continue;

            var peekRng = new Rng(seed, counter);
            int idx = peekRng.NextInt(0, pool.Length);

            string label = group.Key == "colorless"
                ? STS2AdvisorI18n.Pick("    Pick Colorless/Special", "    选无色/特殊牌")
                : STS2AdvisorI18n.Pick($"    Pick {GroupDisplayName(group.Key)} card", $"    选{GroupDisplayName(group.Key)}牌");

            yield return new EventPrediction(label, LocText.Of(pool[idx]), PredictionTag.Warning);
        }
    }

    private static IEnumerable<EventPrediction> PredictArcaneScroll(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var poolModel = owner.Character.CardPool;
        var unlocked = poolModel.GetUnlockedCards(
            owner.UnlockState,
            owner.RunState.CardMultiplayerConstraint);

        var candidates = unlocked
            .Where(c => c != null && c.Rarity == CardRarity.Rare)
            .ToList();

        if (candidates.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Arcane Scroll", "  └ 奥术卷轴"),
                STS2AdvisorI18n.Pick("No Rare cards available in pool.", "卡池中没有可用稀有牌。"),
                PredictionTag.Normal);
            yield break;
        }

        var rewardsRng = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);
        int idx = peekRng.NextInt(0, candidates.Count);

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Rare card", "  └ 稀有牌"),
            LocText.Of(candidates[idx]),
            PredictionTag.Good);
    }

    private static IEnumerable<EventPrediction> PredictMassiveScroll(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var colorless = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(owner.RunState.UnlockState, owner.RunState.CardMultiplayerConstraint);
        var charCards = owner.Character.CardPool
            .GetUnlockedCards(owner.RunState.UnlockState, owner.RunState.CardMultiplayerConstraint);
        var fullPool = colorless.Concat(charCards)
            .Where(c => c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly)
            .ToList();

        if (fullPool.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Massive Scroll", "  └ 巨型卷轴"),
                STS2AdvisorI18n.Pick("No multiplayer-only cards in pool.", "池中没有多人专属卡牌。"),
                PredictionTag.Normal);
            yield break;
        }

        var rewardsRng = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);
        var blacklistIds = new HashSet<string>();
        var results = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            float rarityRoll = peekRng.NextFloat();
            CardRarity rarity;
            if (rarityRoll < CardRarityOdds.RegularRareOdds)
                rarity = CardRarity.Rare;
            else if (rarityRoll < 0.37f)
                rarity = CardRarity.Uncommon;
            else
                rarity = CardRarity.Common;

            var candidates = FilterMPPool(fullPool, rarity, blacklistIds);
            if (candidates.Count == 0) candidates = FilterMPPool(fullPool, CardRarity.Uncommon, blacklistIds);
            if (candidates.Count == 0) candidates = FilterMPPool(fullPool, CardRarity.Rare, blacklistIds);
            if (candidates.Count == 0) candidates = fullPool.Where(c => !blacklistIds.Contains(c.Id.Entry)).ToList();

            if (candidates.Count == 0)
            {
                peekRng.NextInt(1);
                peekRng.NextFloat();
                results.Add("?");
                continue;
            }

            int idx = peekRng.NextInt(0, candidates.Count);
            var card = candidates[idx];
            blacklistIds.Add(card.Id.Entry);
            peekRng.NextFloat();
            results.Add(LocText.Of(card));
        }

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Massive Scroll (choose 1)", "  └ 巨型卷轴（选1张）"),
            string.Join(" / ", results),
            PredictionTag.Good);
    }

    private static List<CardModel> FilterMPPool(
        List<CardModel> pool, CardRarity rarity, HashSet<string> blacklistIds) =>
        pool.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();


    private static string GetPoolKey(CardModel c)
    {
        bool isSpecial = c.Type == CardType.Quest
            || c.Rarity == CardRarity.Event
            || c.Rarity == CardRarity.Ancient
            || c.Rarity == CardRarity.Token;
        return isSpecial ? "colorless" : (c.Pool?.Id.Entry ?? "colorless");
    }

    private static string GroupDisplayName(string poolKey) =>
        poolKey.Replace("_CARD_POOL", "").Replace("_", " ");
}
