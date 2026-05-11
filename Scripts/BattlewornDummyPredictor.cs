using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class BattlewornDummyPredictor : IEventPredictor
{
    public Type EventType => typeof(BattlewornDummy);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var player = eventModel.Owner;
        if (player == null)
            return new();

        var rows = new List<EventPrediction>();

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Setting 1 (Potion)", "设置1（药水）"),
            PredictPotionReward(player),
            PredictionTag.Good));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Setting 2 (Upgrade 2 cards)", "设置2（升级2张牌）"),
            PredictUpgradeTargets(player),
            PredictionTag.Warning));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Setting 3 (Relic)", "设置3（遗物）"),
            STS2AdvisorI18n.Pick("Obtain the next relic from relic queue front.", "获得遗物队列前端的下一个遗物。"),
            PredictionTag.Good));

        return rows;
    }

    private static string PredictPotionReward(Player player)
    {
        var items = player.Character.PotionPool.GetUnlockedPotions(player.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(player.UnlockState))
            .ToArray();

        if (items.Length == 0)
            return STS2AdvisorI18n.Pick("No potion available.", "没有可用药水。");

        var rewardsRng = player.PlayerRng.Rewards;
        var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);
        int idx = peekRng.NextInt(0, items.Length);
        return STS2AdvisorI18n.Pick("Likely potion: ", "可能药水：") + LocText.Of(items[idx]);
    }

    private static string PredictUpgradeTargets(Player player)
    {
        var candidates = PileType.Deck.GetPile(player).Cards
            .Where(c => c?.IsUpgradable ?? false)
            .ToList();

        if (candidates.Count == 0)
            return STS2AdvisorI18n.Pick("No upgradable cards.", "没有可升级卡牌。");
        if (candidates.Count == 1)
            return STS2AdvisorI18n.Pick("Will upgrade: ", "将升级：") + LocText.Of(candidates[0]);
        if (candidates.Count == 2)
            return STS2AdvisorI18n.Pick("Will upgrade: ", "将升级：")
                + string.Join(" / ", candidates.Select(LocText.Of));

        // Mirror StableShuffle(RunState.Rng.Niche).Take(2)
        var niche = player.RunState.Rng.Niche;
        var peekRng = new Rng(niche.Seed, niche.Counter);
        var shuffled = StableShuffle(candidates, peekRng);
        var picked = shuffled.Take(2).Select(LocText.Of).ToList();
        return STS2AdvisorI18n.Pick("Will upgrade: ", "将升级：") + string.Join(" / ", picked);
    }

    private static List<MegaCrit.Sts2.Core.Models.CardModel> StableShuffle(
        List<MegaCrit.Sts2.Core.Models.CardModel> source, Rng rng)
    {
        var list = source.ToList();
        var sorted = list.ToList();
        sorted.Sort();
        for (int i = 0; i < list.Count; i++)
            list[i] = sorted[i];

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int j = rng.NextInt(n + 1);
            (list[j], list[n]) = (list[n], list[j]);
        }

        return list;
    }
}
