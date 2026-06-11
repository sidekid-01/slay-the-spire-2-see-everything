using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class PotionCourierPredictor : IEventPredictor
{
    public Type EventType => typeof(PotionCourier);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Grab Potions", "拿药水"),
                STS2AdvisorI18n.Pick("Gain 3 Foul Potions.", "获得 3 瓶污秽药水。"),
                PredictionTag.Warning)
        };

        string ransackResult = PredictRansackPotion(owner);
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Ransack", "洗劫"),
            STS2AdvisorI18n.Pick("Will gain Uncommon potion: ", "将获得非凡药水：") + ransackResult,
            PredictionTag.Good));

        return rows;
    }

    private static string PredictRansackPotion(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var uncommon = owner.Character.PotionPool
            .GetUnlockedPotions(owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(owner.UnlockState))
            .Where(p => p.Rarity == PotionRarity.Uncommon)
            .ToList();

        if (uncommon.Count == 0)
            return STS2AdvisorI18n.Pick("No valid potion.", "没有可用药水。");

        var rewards = owner.PlayerRng.Rewards;
        var peekRng = new Rng(rewards.Seed, rewards.Counter);
        int index = peekRng.NextInt(0, uncommon.Count);
        return LocText.Of(uncommon[index]);
    }
}
