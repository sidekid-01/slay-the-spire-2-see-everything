using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class CrystalSpherePredictor : IEventPredictor
{
    public Type EventType => typeof(CrystalSphere);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int cost = ReadIntVar(eventModel, "UncoverFutureCost", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Uncover Future", "揭示未来"),
                STS2AdvisorI18n.Pick(
                    $"Pay {cost} gold, then play Crystal Sphere minigame with 3 rounds.",
                    $"支付 {cost} 金币，然后进行水晶球小游戏（3轮）。"),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Payment Plan", "分期方案"),
                STS2AdvisorI18n.Pick(
                    "Add Debt curse to deck, then play Crystal Sphere minigame with 6 rounds.",
                    "向牌组加入 Debt 诅咒，然后进行水晶球小游戏（6轮）。"),
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
