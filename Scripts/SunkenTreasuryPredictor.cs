using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class SunkenTreasuryPredictor : IEventPredictor
{
    public Type EventType => typeof(SunkenTreasury);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int smallGold = ReadIntVar(eventModel, "SmallChestGold", 0);
        int largeGold = ReadIntVar(eventModel, "LargeChestGold", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("First Chest", "第一个宝箱"),
                STS2AdvisorI18n.Pick(
                    $"Gain {smallGold} gold.",
                    $"获得 {smallGold} 金币。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Second Chest", "第二个宝箱"),
                STS2AdvisorI18n.Pick(
                    $"Gain {largeGold} gold, then add Greed curse to deck.",
                    $"获得 {largeGold} 金币，然后向牌组加入 Greed 诅咒。"),
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
