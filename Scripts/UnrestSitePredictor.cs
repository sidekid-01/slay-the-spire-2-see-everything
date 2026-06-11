using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class UnrestSitePredictor : IEventPredictor
{
    public Type EventType => typeof(UnrestSite);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int heal = eventModel.DynamicVars.Heal.IntValue;
        int maxHpLoss = ReadIntVar(eventModel, "MaxHpLoss", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Rest", "休息"),
                STS2AdvisorI18n.Pick(
                    $"Heal {heal}, then add PoorSleep curse to deck.",
                    $"回复 {heal} 生命，然后向牌组加入 PoorSleep 诅咒。"),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Kill", "击杀"),
                STS2AdvisorI18n.Pick(
                    $"Lose {maxHpLoss} Max HP, then obtain next relic from relic queue front.",
                    $"失去 {maxHpLoss} 点最大生命，然后获得遗物队列前端下一个遗物。"),
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
