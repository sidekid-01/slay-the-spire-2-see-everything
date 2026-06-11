using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class WellspringPredictor : IEventPredictor
{
    public Type EventType => typeof(Wellspring);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var potionPool = owner.Character.PotionPool
            .GetUnlockedPotions(owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(owner.UnlockState))
            .ToList();

        string potionText;
        if (potionPool.Count == 0)
        {
            potionText = STS2AdvisorI18n.Pick("No potion available.", "没有可用药水。");
        }
        else
        {
            var rewards = owner.PlayerRng.Rewards;
            var peekRng = new Rng(rewards.Seed, rewards.Counter);
            int idx = peekRng.NextInt(0, potionPool.Count);
            potionText = LocText.Of(potionPool[idx]);
        }

        int batheCurses = ReadIntVar(eventModel, "BatheCurses", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Bottle", "装瓶"),
                STS2AdvisorI18n.Pick(
                    $"Gain potion: {potionText}",
                    $"获得药水：{potionText}"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Bathe", "沐浴"),
                STS2AdvisorI18n.Pick(
                    $"Remove 1 chosen card, then add {batheCurses} Guilty curse card(s).",
                    $"移除 1 张自选卡牌，然后加入 {batheCurses} 张 Guilty 诅咒牌。"),
                PredictionTag.Warning)
        };
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;
        return eventModel.DynamicVars[key].IntValue;
    }
}
