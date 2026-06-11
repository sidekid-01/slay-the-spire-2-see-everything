using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class ThisOrThatPredictor : IEventPredictor
{
    public Type EventType => typeof(ThisOrThat);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int hpLoss = ReadIntVar(eventModel, "HpLoss");
        int gold = ReadIntVar(eventModel, "Gold");

        string plainText;
        if (hpLoss > 0 && gold > 0)
        {
            plainText = STS2AdvisorI18n.Pick(
                $"Take {hpLoss} damage and gain {gold} gold.",
                $"受到 {hpLoss} 点伤害并获得 {gold} 金币。");
        }
        else if (hpLoss > 0)
        {
            plainText = STS2AdvisorI18n.Pick($"Take {hpLoss} damage.", $"受到 {hpLoss} 点伤害。");
        }
        else if (gold > 0)
        {
            plainText = STS2AdvisorI18n.Pick($"Gain {gold} gold.", $"获得 {gold} 金币。");
        }
        else
        {
            plainText = STS2AdvisorI18n.Pick("Take damage and gain gold.", "受到伤害并获得金币。");
        }

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Plain", "朴素"),
                plainText,
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Ornate", "华丽"),
                STS2AdvisorI18n.Pick(
                    "Obtain next relic from relic queue front, then add Clumsy to deck.",
                    "获得遗物队列前端的下一个遗物，然后向牌组加入 1 张笨拙。"),
                PredictionTag.Warning)
        };
    }

    private static int ReadIntVar(EventModel eventModel, string key)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return 0;
        return eventModel.DynamicVars[key].IntValue;
    }
}
