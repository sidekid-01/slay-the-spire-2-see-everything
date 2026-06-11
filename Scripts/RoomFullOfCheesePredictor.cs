using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class RoomFullOfCheesePredictor : IEventPredictor
{
    public Type EventType => typeof(RoomFullOfCheese);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Gorge", "狼吞虎咽"),
                STS2AdvisorI18n.Pick("Generate 8 Common class cards, choose 2 to add to deck.", "生成 8 张职业普通牌，选择 2 张加入牌组。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Search", "搜寻"),
                STS2AdvisorI18n.Pick("Take 14 damage and obtain Chosen Cheese.", "受到 14 点伤害，并获得精选奶酪。"),
                PredictionTag.Warning)
        };
    }
}
