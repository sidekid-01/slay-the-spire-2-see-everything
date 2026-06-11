using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        // Neow 的选项在 BeginEvent 时通常已经生成；直接读取“游戏当前三选项”可避免 RNG/版本漂移
        var live = TryGetLiveInitialOptions(neow);
        if (live != null && live.Count > 0)
            return PredictFromLiveOptions(live, owner);

        // 读不到再 fallback 到模拟（可能因 RNG 差异不完全一致）
        return PredictStandardFlow(neow, owner, mirrorRng);
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

    private static List<EventPrediction> PredictStandardFlow(
        Neow neow,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        Rng mirrorRng)
    {
        var cursePool = BuildCursePool(neow, owner);
        if (cursePool.Count == 0)
            return new()
            {
                new EventPrediction(
                    STS2AdvisorI18n.Pick("Neow options", "Neow 选项"),
                    STS2AdvisorI18n.Pick("Unable to read Neow option pool.", "无法读取 Neow 选项池。"),
                    PredictionTag.Warning)
            };

        int curseIndex = mirrorRng.NextInt(0, cursePool.Count);
        var cursedOption = cursePool[curseIndex];
        var cursedRelic = GetEventOptionRelic(cursedOption);

        var positivePool = BuildPositivePool(neow, owner, cursedRelic, mirrorRng);

        // list2.ToList().UnstableShuffle(base.Rng).Take(2)
        var shuffled = UnstableShuffle(positivePool.ToList(), mirrorRng);
        var selectedPositive = shuffled.Take(2).ToList();

        var rows = new List<EventPrediction>();

        // 游戏显示顺序：先两个正向选项，再诅咒选项
        for (int i = 0; i < selectedPositive.Count; i++)
        {
            var option = selectedPositive[i];
            var relic = GetEventOptionRelic(option);
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Positive option {i + 1}", $"正向选项 {i + 1}"),
                DescribeEventOption(option, owner),
                PredictionTag.Good));

            if (relic is NewLeaf)
                rows.AddRange(PredictNewLeafTransform(owner));
            if (relic is ArcaneScroll)
                rows.AddRange(PredictArcaneScroll(owner));
            if (relic is MassiveScroll)
                rows.AddRange(PredictMassiveScroll(owner));
            if (relic is LostCoffer)
                rows.AddRange(PredictLostCoffer(owner));
            if (relic is SmallCapsule)
                rows.AddRange(PredictSmallCapsule(owner));
            if (relic is LeadPaperweight)
                rows.AddRange(PredictLeadPaperweight(owner));
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Cursed choice", "诅咒选项"),
            DescribeEventOption(cursedOption),
            PredictionTag.Bad));

        if (cursedRelic is LeafyPoultice)
            rows.AddRange(PredictLeafyPoultice(owner));
        if (cursedRelic is LargeCapsule)
            rows.AddRange(PredictLargeCapsule(owner));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Display order", "显示顺序"),
            STS2AdvisorI18n.Pick(
                "In game: two positive options are shown first, then cursed option.",
                "游戏中显示顺序：先两个正向选项，再诅咒选项。"),
            PredictionTag.Normal));

        return rows;
    }

    private static IReadOnlyList<EventOption>? TryGetLiveInitialOptions(Neow neow)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 先按常见命名在“继承链”上查找，命中就返回
        foreach (var t in EnumerateTypeHierarchy(neow.GetType()))
        {
            foreach (var name in new[]
            {
                "InitialOptions", "InitialOptionList", "Options", "EventOptions", "_options", "_initialOptions"
            })
            {
                var field = t.GetField(name, flags);
                if (field != null)
                {
                    try
                    {
                        var cand = CoerceEventOptionList(field.GetValue(neow));
                        if (cand != null && cand.Count > 0)
                            return cand;
                    }
                    catch { }
                }

                var prop = t.GetProperty(name, flags);
                if (prop != null && prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        var cand = CoerceEventOptionList(prop.GetValue(neow));
                        if (cand != null && cand.Count > 0)
                            return cand;
                    }
                    catch { }
                }
            }
        }

        // 兜底：全扫描，但用打分避免误选（比如 CurseOptions/PositiveOptions 这种“池”）
        (IReadOnlyList<EventOption>? list, int score, string? name) best = (null, int.MinValue, null);

        foreach (var t in EnumerateTypeHierarchy(neow.GetType()))
        {
            foreach (var f in t.GetFields(flags))
            {
                object? v;
                try { v = f.GetValue(neow); } catch { continue; }
                var cand = CoerceEventOptionList(v);
                if (cand == null || cand.Count == 0) continue;

                int score = ScoreLiveOptionsCandidate(f.Name, cand);
                if (score > best.score)
                    best = (cand, score, f.Name);
            }

            foreach (var p in t.GetProperties(flags))
            {
                if (p.GetIndexParameters().Length != 0) continue;
                object? v;
                try { v = p.GetValue(neow); } catch { continue; }
                var cand = CoerceEventOptionList(v);
                if (cand == null || cand.Count == 0) continue;

                int score = ScoreLiveOptionsCandidate(p.Name, cand);
                if (score > best.score)
                    best = (cand, score, p.Name);
            }
        }

        return best.list;
    }

    private static IEnumerable<Type> EnumerateTypeHierarchy(Type t)
    {
        for (var cur = t; cur != null; cur = cur.BaseType)
            yield return cur;
    }

    private static int ScoreLiveOptionsCandidate(string memberName, IReadOnlyList<EventOption> list)
    {
        // 我们目标：当前 UI 展示的“最终选项列表”通常是 3 个；而池子往往更大且名字包含 Curse/Positive。
        int score = 0;
        if (list.Count == 3) score += 100;
        if (list.Count is > 0 and <= 5) score += 10;

        var n = memberName.ToLowerInvariant();
        if (n.Contains("initial")) score += 40;
        if (n.Contains("option")) score += 20;
        if (n.Contains("options")) score += 15;
        if (n.Contains("_options") || n.Contains("_initial")) score += 10;

        // 强力惩罚：这些更像“池”或辅助列表
        if (n.Contains("curse")) score -= 80;
        if (n.Contains("positive")) score -= 80;
        if (n.Contains("modifier")) score -= 80;
        if (n.Contains("allpossible")) score -= 200;

        // 轻度校验：标题应可读（避免拿到空壳/占位列表）
        int goodTitle = 0;
        for (int i = 0; i < list.Count; i++)
        {
            try
            {
                var title = LocText.Of(list[i].Title);
                if (!string.IsNullOrWhiteSpace(title)) goodTitle++;
            }
            catch { }
        }
        score += goodTitle * 3;

        return score;
    }

    private static IReadOnlyList<EventOption>? CoerceEventOptionList(object? v)
    {
        if (v is IReadOnlyList<EventOption> ro)
            return ro;
        if (v is EventOption[] arr)
            return arr;
        if (v is List<EventOption> list)
            return list;
        return null;
    }

    private static List<EventPrediction> PredictFromLiveOptions(
        IReadOnlyList<EventOption> options,
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        // 游戏返回的列表里：两个正向在前，诅咒在后（你贴的 GenerateInitialOptions 就是最后 Add(eventOption)）
        var rows = new List<EventPrediction>();

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            var relic = opt.Relic;
            bool isCursed = relic is CursedPearl or PrecariousShears or LeafyPoultice or LargeCapsule
                            || relic is ScrollBoxes // BundleOption 也是从 CurseOptions 里来的
                            || relic is SilverCrucible; // EmpowerOption 旧实现是 relic；新版不一定，但保守标记

            string label = isCursed
                ? STS2AdvisorI18n.Pick("Cursed choice", "诅咒选项")
                : STS2AdvisorI18n.Pick($"Positive option {i + 1}", $"正向选项 {i + 1}");

            var tag = isCursed ? PredictionTag.Bad : PredictionTag.Good;
            rows.Add(new EventPrediction(label, DescribeEventOption(opt, owner), tag));

            // 只对“遗物型”的效果做二级预测（非遗物选项只展示标题/描述）
            if (relic is LeafyPoultice) rows.AddRange(PredictLeafyPoultice(owner));
            if (relic is LargeCapsule) rows.AddRange(PredictLargeCapsule(owner));
            if (relic is NewLeaf) rows.AddRange(PredictNewLeafTransform(owner));
            if (relic is ArcaneScroll) rows.AddRange(PredictArcaneScroll(owner));
            if (relic is MassiveScroll) rows.AddRange(PredictMassiveScroll(owner));
            if (relic is LostCoffer) rows.AddRange(PredictLostCoffer(owner));
            if (relic is SmallCapsule) rows.AddRange(PredictSmallCapsule(owner));
            if (relic is LeadPaperweight) rows.AddRange(PredictLeadPaperweight(owner));
        }

        return rows;
    }

    private static List<EventOption> BuildCursePool(
        Neow neow,
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var list = GetOptionListField(neow, "CurseOptions");

        if (ScrollBoxes.CanGenerateBundles(owner))
            AddOptionFieldIfExists(neow, "BundleOption", list);
        if (owner.RunState.Players.Count == 1)
            AddOptionFieldIfExists(neow, "EmpowerOption", list);

        return list;
    }

    private static List<EventOption> BuildPositivePool(
        Neow neow,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        RelicModel? cursed,
        Rng mirrorRng)
    {
        var list = GetOptionListField(neow, "PositiveOptions");

        if (cursed is CursedPearl)
            list.RemoveAll(o => GetEventOptionRelic(o) is GoldenPearl);
        if (cursed is PrecariousShears)
            list.RemoveAll(o => GetEventOptionRelic(o) is PreciseScissors);
        if (cursed is LeafyPoultice)
            list.RemoveAll(o => GetEventOptionRelic(o) is NewLeaf);

        if (owner.RunState.Players.Count > 1)
            AddOptionFieldIfExists(neow, "ClericOption", list);

        if (mirrorRng.NextBool())
            AddOptionFieldIfExists(neow, "ToughnessOption", list);
        else
            AddOptionFieldIfExists(neow, "SafetyOption", list);

        if (cursed is not LargeCapsule)
        {
            if (mirrorRng.NextBool())
                AddOptionFieldIfExists(neow, "PatienceOption", list);
            else
                AddOptionFieldIfExists(neow, "ScavengerOption", list);
        }

        return list;
    }

    private static List<EventOption> UnstableShuffle(List<EventOption> list, Rng rng)
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

    private static string DescribeEventOption(
        EventOption option,
        MegaCrit.Sts2.Core.Entities.Players.Player? owner = null)
    {
        var relic = option.Relic;
        if (relic == null)
            return LocText.Of(option.Title);

        string name = RelicName(relic);
        return relic switch
        {
            NewLeaf => name + STS2AdvisorI18n.Pick(" (Transforms 1 card)", "（变形 1 张牌）"),
            ArcaneScroll => name + STS2AdvisorI18n.Pick(" (Gain 1 Rare card)", "（获得 1 张稀有牌）"),
            MassiveScroll => name + STS2AdvisorI18n.Pick(" (Offer 3 cards, choose 1)", "（提供3张牌，选择1张）"),
            LostCoffer => name + STS2AdvisorI18n.Pick(" (3 cards + 1 potion)", "（3张牌 + 1药水）"),
            SmallCapsule => name + STS2AdvisorI18n.Pick(" (1 relic from queue front)", "（从遗物队列前端获得1个遗物）"),
            LeadPaperweight => name + STS2AdvisorI18n.Pick(" (Choose 1 of 2 colorless cards)", "（从2张无色牌中选1张）"),
            _ => name
        };
    }

    private static List<EventOption> GetOptionListField(Neow neow, string fieldName)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var field = typeof(Neow).GetField(fieldName, flags);
        if (field?.GetValue(neow) is IEnumerable<EventOption> fieldOptions)
            return fieldOptions.ToList();

        var prop = typeof(Neow).GetProperty(fieldName, flags);
        if (prop?.GetValue(neow) is IEnumerable<EventOption> propOptions)
            return propOptions.ToList();

        return new List<EventOption>();
    }

    private static void AddOptionFieldIfExists(Neow neow, string fieldName, List<EventOption> target)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var field = typeof(Neow).GetField(fieldName, flags);
        if (field?.GetValue(neow) is EventOption option)
        {
            target.Add(option);
            return;
        }

        var prop = typeof(Neow).GetProperty(fieldName, flags);
        if (prop?.GetValue(neow) is EventOption propOption)
            target.Add(propOption);
    }

    private static RelicModel? GetEventOptionRelic(EventOption option) => option.Relic;

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

    private static IEnumerable<EventPrediction> PredictLargeCapsule(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        // Cards: +Strike +Defend (character basics) added to deck
        var charCards = owner.Character.CardPool.AllCards;
        var strikeCard = charCards.FirstOrDefault(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike));
        var defendCard = charCards.FirstOrDefault(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Defend));
        var cardNames = new List<string>();
        if (strikeCard != null) cardNames.Add(LocText.Of(strikeCard));
        if (defendCard != null) cardNames.Add(LocText.Of(defendCard));
        string deckChange = cardNames.Count > 0
            ? "+" + string.Join(" +", cardNames)
            : STS2AdvisorI18n.Pick("(no basic cards found)", "（未找到基础卡）");
        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Cards added", "  └ 加入牌组"),
            deckChange,
            PredictionTag.Normal);

        // Relics: mirrors RelicFactory.PullNextRelicFromFront(player) x2
        // Rarity is determined by PlayerRng.Rewards.NextFloat() (same as RelicFactory.RollRarity)
        var rewardsRng = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);

        for (int pull = 0; pull < 2; pull++)
        {
            var (relicName, rarityLabel) = PeekNextRelicFromQueue(owner, peekRng, out _);
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick($"  └ Relic {pull + 1} ({rarityLabel})", $"  └ 遗物 {pull + 1}（{rarityLabel}）"),
                relicName,
                PredictionTag.Good);
        }
    }

    private static IEnumerable<EventPrediction> PredictLeafyPoultice(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Leafy Poultice", "  └ 树叶药膏"),
            STS2AdvisorI18n.Pick("-10 Max HP, then transform 1 Strike + 1 Defend (if found).", "最大生命值 -10，然后各变形 1 张打击与防御（若存在）。"),
            PredictionTag.Bad);

        var basicCards = owner.Deck.Cards.Where(c => c.Rarity == CardRarity.Basic).ToList();
        var strike = basicCards.FirstOrDefault(c => c.Tags.Contains(CardTag.Strike));
        var defend = basicCards.FirstOrDefault(c => c.Tags.Contains(CardTag.Defend));

        if (strike == null && defend == null)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Transform result", "  └ 变形结果"),
                EventPredictionText.NoTransformableCards(),
                PredictionTag.Normal);
            yield break;
        }

        var transRng = owner.PlayerRng.Transformations;
        var peekRng = new Rng(transRng.Seed, transRng.Counter);

        if (strike != null)
        {
            var strikePool = TransformPredictor.GetFilteredPool(strike, isInCombat: false);
            if (strikePool.Length == 0)
            {
                yield return new EventPrediction(
                    STS2AdvisorI18n.Pick("    Strike ->", "    打击 ->"),
                    EventPredictionText.NoTransformTargets(),
                    PredictionTag.Normal);
            }
            else
            {
                int strikeIdx = peekRng.NextInt(0, strikePool.Length);
                yield return new EventPrediction(
                    STS2AdvisorI18n.Pick("    Strike ->", "    打击 ->"),
                    LocText.Of(strikePool[strikeIdx]),
                    PredictionTag.Warning);
            }
        }

        if (defend != null)
        {
            var defendPool = TransformPredictor.GetFilteredPool(defend, isInCombat: false);
            if (defendPool.Length == 0)
            {
                yield return new EventPrediction(
                    STS2AdvisorI18n.Pick("    Defend ->", "    防御 ->"),
                    EventPredictionText.NoTransformTargets(),
                    PredictionTag.Normal);
            }
            else
            {
                int defendIdx = peekRng.NextInt(0, defendPool.Length);
                yield return new EventPrediction(
                    STS2AdvisorI18n.Pick("    Defend ->", "    防御 ->"),
                    LocText.Of(defendPool[defendIdx]),
                    PredictionTag.Warning);
            }
        }
    }

    private static IEnumerable<EventPrediction> PredictLeadPaperweight(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        // LeadPaperweight: ColorlessCardPool, CardCreationSource.Other, RegularEncounter, 2 cards
        // RNG: PlayerRng.Rewards — per card: NextFloat(rarity) + NextInt(card) + NextFloat(upgrade)
        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();

        if (colorlessCards.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Lead Paperweight cards", "  └ 铅镇纸 牌"),
                STS2AdvisorI18n.Pick("No colorless cards available.", "无色卡池为空。"),
                PredictionTag.Normal);
            yield break;
        }

        var rewardsRng = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);

        var blacklistIds = new HashSet<string>();
        var cardResults = new List<string>();

        for (int i = 0; i < 2; i++)
        {
            // 1. RollWithBaseOdds(RegularEncounter): NextFloat()
            float rarityRoll = peekRng.NextFloat();
            CardRarity rarity = rarityRoll < CardRarityOdds.RegularRareOdds ? CardRarity.Rare
                              : rarityRoll < 0.37f ? CardRarity.Uncommon
                              : CardRarity.Common;

            // 稀有度が空なら Common → Uncommon → Rare へ昇格
            var candidates = colorlessCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            if (candidates.Count == 0 && rarity == CardRarity.Common)
            {
                rarity = CardRarity.Uncommon;
                candidates = colorlessCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            }
            if (candidates.Count == 0 && rarity == CardRarity.Uncommon)
            {
                rarity = CardRarity.Rare;
                candidates = colorlessCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            }
            if (candidates.Count == 0)
                candidates = colorlessCards.Where(c => !blacklistIds.Contains(c.Id.Entry)).ToList();

            string cardName;
            if (candidates.Count == 0)
            {
                peekRng.NextInt(1);
                peekRng.NextFloat();
                cardName = "?";
            }
            else
            {
                // 2. NextItem(items) = NextInt(0, count)
                int idx = peekRng.NextInt(0, candidates.Count);
                var card = candidates[idx];
                blacklistIds.Add(card.Id.Entry);
                // 3. RollForUpgrade: NextFloat()
                peekRng.NextFloat();
                cardName = LocText.Of(card);
            }
            cardResults.Add(cardName);
        }

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Colorless cards offered (choose 1 or skip)", "  └ 提供的无色牌（选1张或跳过）"),
            string.Join(" / ", cardResults),
            PredictionTag.Good);
    }

    private static IEnumerable<EventPrediction> PredictSmallCapsule(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        // SmallCapsule: RelicReward(player) → RelicFactory.PullNextRelicFromFront(player)
        // = RollRarity(PlayerRng.Rewards) + RelicGrabBag.PullFromFront(rarity)
        var (relicName, rarityLabel) = PeekNextRelicFromQueue(owner, new Rng(owner.PlayerRng.Rewards.Seed, owner.PlayerRng.Rewards.Counter), out _);
        yield return new EventPrediction(
            STS2AdvisorI18n.Pick($"  └ Relic ({rarityLabel})", $"  └ 遗物（{rarityLabel}）"),
            relicName,
            PredictionTag.Good);
    }

    /// <summary>
    /// Peeks one relic pull from the grab bag front using the given peekRng (mirrors RelicFactory.PullNextRelicFromFront).
    /// Returns (relicName, rarityLabel). Advances peekRng by 1 NextFloat call.
    /// outSimDeques is a mutable copy of the deques after this pull (pass null to skip building it).
    /// </summary>
    private static (string relicName, string rarityLabel) PeekNextRelicFromQueue(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        Rng peekRng,
        out Dictionary<RelicRarity, List<RelicModel>>? simDeques)
    {
        simDeques = null;
        var dequesField = typeof(RelicGrabBag)
            .GetField("_deques", BindingFlags.NonPublic | BindingFlags.Instance);
        if (dequesField?.GetValue(owner.RelicGrabBag) is not Dictionary<RelicRarity, List<RelicModel>> rawDeques)
            return (STS2AdvisorI18n.Pick("Unable to read relic queue.", "无法读取遗物队列。"), "");

        var localDeques = new Dictionary<RelicRarity, List<RelicModel>>();
        foreach (var kv in rawDeques)
            localDeques[kv.Key] = kv.Value.Where(r => r.IsAllowed(owner.RunState)).ToList();
        simDeques = localDeques;

        float rarityRoll = peekRng.NextFloat();
        RelicRarity rarity = rarityRoll < 0.5f ? RelicRarity.Common
                           : rarityRoll < 0.83f ? RelicRarity.Uncommon
                           : RelicRarity.Rare;

        RelicModel? relic = null;
        RelicRarity current = rarity;
        while (current != RelicRarity.None)
        {
            if (localDeques.TryGetValue(current, out var deque) && deque.Count > 0)
            {
                relic = deque[0];
                deque.RemoveAt(0);
                break;
            }
            current = current switch
            {
                RelicRarity.Common => RelicRarity.Uncommon,
                RelicRarity.Uncommon => RelicRarity.Rare,
                _ => RelicRarity.None
            };
        }

        string relicName = relic != null
            ? LocText.Of(relic)
            : STS2AdvisorI18n.Pick("(Circlet fallback)", "（备用圆环）");
        string rarityLabel = rarity switch
        {
            RelicRarity.Common   => STS2AdvisorI18n.Pick("Common",   "普通"),
            RelicRarity.Uncommon => STS2AdvisorI18n.Pick("Uncommon", "稀有"),
            RelicRarity.Rare     => STS2AdvisorI18n.Pick("Rare",     "传说"),
            _                    => ""
        };
        return (relicName, rarityLabel);
    }

    private static IEnumerable<EventPrediction> PredictLostCoffer(
        MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        // Pool: character cards excluding Basic / Ancient (mirrors CardFactory.CreateForReward filter)
        var charCards = owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();

        if (charCards.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Lost Coffer cards", "  └ 遗失保险箱 牌"),
                STS2AdvisorI18n.Pick("No cards available in pool.", "卡池中没有可用牌。"),
                PredictionTag.Normal);
            yield break;
        }

        // Peek PlayerRng.Rewards — consumed in order: (rarity+card+upgrade) × 3, then rarity+potion
        var rewardsRng = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);

        var blacklistIds = new HashSet<string>();
        var cardResults = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            // 1. RollWithBaseOdds(RegularEncounter): one NextFloat()
            float rarityRoll = peekRng.NextFloat();
            CardRarity rarity = rarityRoll < CardRarityOdds.RegularRareOdds ? CardRarity.Rare
                              : rarityRoll < 0.37f ? CardRarity.Uncommon
                              : CardRarity.Common;

            // Escalate if no candidates: Common → Uncommon → Rare (mirrors RollForRarity + GetNextHighestRarity)
            var candidates = charCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            if (candidates.Count == 0 && rarity == CardRarity.Common)
            {
                rarity = CardRarity.Uncommon;
                candidates = charCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            }
            if (candidates.Count == 0 && rarity == CardRarity.Uncommon)
            {
                rarity = CardRarity.Rare;
                candidates = charCards.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
            }
            if (candidates.Count == 0)
                candidates = charCards.Where(c => !blacklistIds.Contains(c.Id.Entry)).ToList();

            string cardName;
            if (candidates.Count == 0)
            {
                // Pool exhausted — still consume expected RNG calls
                peekRng.NextInt(1);
                peekRng.NextFloat();
                cardName = "?";
            }
            else
            {
                // 2. NextItem(items) = NextInt(0, count)
                int idx = peekRng.NextInt(0, candidates.Count);
                var card = candidates[idx];
                blacklistIds.Add(card.Id.Entry);

                // 3. RollForUpgrade: one NextFloat() (baseChance=0 at Neow, Act 0)
                peekRng.NextFloat();
                cardName = LocText.Of(card);
            }
            cardResults.Add(cardName);
        }

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Cards offered (choose 1)", "  └ 提供的牌（选1张）"),
            string.Join(" / ", cardResults),
            PredictionTag.Good);

        // Potion: PotionFactory.CreateRandomPotionOutOfCombat(player, PlayerRng.Rewards)
        // continues peeking from where the 3 card rolls left off
        var allPotions = PotionFactory.GetPotionOptions(owner, System.Array.Empty<PotionModel>()).ToList();
        if (allPotions.Count == 0)
        {
            yield return new EventPrediction(
                STS2AdvisorI18n.Pick("  └ Potion offered", "  └ 提供的药水"),
                STS2AdvisorI18n.Pick("No potion available.", "没有可用药水。"),
                PredictionTag.Normal);
            yield break;
        }

        // PotionFactory.CreateRandomPotion: NextFloat (rarity), then NextItem = NextInt(0, count)
        float potionRarityRoll = peekRng.NextFloat();
        PotionRarity potionRarity = potionRarityRoll <= 0.1f ? PotionRarity.Rare
                                  : potionRarityRoll <= 0.35f ? PotionRarity.Uncommon
                                  : PotionRarity.Common;
        var potionCandidates = allPotions.Where(p => p.Rarity == potionRarity).ToList();

        string potionName;
        if (potionCandidates.Count == 0)
        {
            potionName = STS2AdvisorI18n.Pick("(no potion of that rarity)", "（该稀有度无药水）");
        }
        else
        {
            int pIdx = peekRng.NextInt(0, potionCandidates.Count);
            potionName = LocText.Of(potionCandidates[pIdx]);
        }

        yield return new EventPrediction(
            STS2AdvisorI18n.Pick("  └ Potion offered", "  └ 提供的药水"),
            potionName,
            PredictionTag.Good);
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
