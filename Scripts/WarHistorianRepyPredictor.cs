using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class WarHistorianRepyPredictor : IEventPredictor
{
    public Type EventType => typeof(WarHistorianRepy);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Unlock Cage", "打开牢笼"),
                STS2AdvisorI18n.Pick(
                    "Consume Lantern Key, set FreedRepy=true, and obtain HistoryCourse relic.",
                    "消耗 Lantern Key，设置 FreedRepy=true，并获得 HistoryCourse 遗物。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Unlock Chest", "打开宝箱"),
                STS2AdvisorI18n.Pick(
                    "Consume Lantern Key, then gain custom rewards: 2 potions + 2 relic rewards.",
                    "消耗 Lantern Key，然后获得自定义奖励：2 个药水奖励 + 2 个遗物奖励。"),
                PredictionTag.Warning)
        };
    }
}
