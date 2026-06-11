using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class LostWispPredictor : IEventPredictor
{
    public Type EventType => typeof(LostWisp);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int gold = eventModel.DynamicVars.Gold.IntValue;

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Claim", "领取"),
                STS2AdvisorI18n.Pick(
                    "Add Decay curse to deck, then obtain Lost Wisp relic.",
                    "向牌组加入 Decay 诅咒，然后获得 Lost Wisp 遗物。"),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Search", "搜寻"),
                STS2AdvisorI18n.Pick(
                    $"Gain {gold} gold.",
                    $"获得 {gold} 金币。"),
                PredictionTag.Good)
        };
    }
}
