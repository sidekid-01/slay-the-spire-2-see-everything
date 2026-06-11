using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class TheLanternKeyPredictor : IEventPredictor
{
    public Type EventType => typeof(TheLanternKey);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int gold = eventModel.DynamicVars.Gold.IntValue;

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Return the Key", "归还钥匙"),
                STS2AdvisorI18n.Pick(
                    $"Gain {gold} gold and end event.",
                    $"获得 {gold} 金币并结束事件。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Keep the Key", "留下钥匙"),
                STS2AdvisorI18n.Pick(
                    "Enter Mysterious Knight event combat; on win, receive special card reward: LanternKey.",
                    "进入神秘骑士事件战斗；获胜后获得特殊卡牌奖励：LanternKey。"),
                PredictionTag.Warning)
        };
    }
}
