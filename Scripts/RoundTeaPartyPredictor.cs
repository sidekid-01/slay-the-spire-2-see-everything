using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class RoundTeaPartyPredictor : IEventPredictor
{
    public Type EventType => typeof(RoundTeaParty);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int damage = eventModel.DynamicVars.Damage.IntValue;

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Enjoy Tea", "享用茶会"),
                STS2AdvisorI18n.Pick(
                    "Obtain RoyalPoison relic and fully heal to max HP.",
                    "获得 RoyalPoison 遗物，并回复至满生命。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Pick Fight", "挑衅开战"),
                STS2AdvisorI18n.Pick(
                    $"Then continue: take {damage} damage and obtain next relic from relic queue front.",
                    $"随后继续：受到 {damage} 点伤害，并获得遗物队列前端下一个遗物。"),
                PredictionTag.Warning)
        };
    }
}
