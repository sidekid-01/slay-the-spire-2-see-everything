using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class SpiritGrafterPredictor : IEventPredictor
{
    public Type EventType => typeof(SpiritGrafter);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int heal = ReadIntVar(eventModel, "LetItInHealAmount", 0);
        int hpLoss = ReadIntVar(eventModel, "RejectionHpLoss", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Let It In", "让其进入"),
                STS2AdvisorI18n.Pick(
                    $"Heal {heal}, then add Metamorphosis to deck.",
                    $"回复 {heal} 生命，然后向牌组加入 Metamorphosis。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Rejection", "拒绝"),
                STS2AdvisorI18n.Pick(
                    $"Remove 1 chosen card from deck, then take {hpLoss} damage.",
                    $"从牌组移除 1 张自选卡牌，然后受到 {hpLoss} 点伤害。"),
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
