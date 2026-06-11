using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class TheLegendsWereTruePredictor : IEventPredictor
{
    public Type EventType => typeof(TheLegendsWereTrue);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int damage = eventModel.DynamicVars.Damage.IntValue;

        var potions = owner.Character.PotionPool
            .GetUnlockedPotions(owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(owner.UnlockState))
            .ToList();

        string potionText;
        if (potions.Count == 0)
        {
            potionText = STS2AdvisorI18n.Pick("No potion available.", "没有可用药水。");
        }
        else
        {
            var rewards = owner.PlayerRng.Rewards;
            var peekRng = new Rng(rewards.Seed, rewards.Counter);
            int idx = peekRng.NextInt(0, potions.Count);
            potionText = LocText.Of(potions[idx]);
        }

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Nab the Map", "抢走地图"),
                STS2AdvisorI18n.Pick(
                    "Add SpoilsMap to deck.",
                    "向牌组加入 SpoilsMap。"),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Slowly Find an Exit", "慢慢找出口"),
                STS2AdvisorI18n.Pick(
                    $"Take {damage} damage, then gain potion: {potionText}",
                    $"受到 {damage} 点伤害，然后获得药水：{potionText}"),
                PredictionTag.Warning)
        };
    }
}
