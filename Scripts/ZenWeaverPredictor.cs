using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class ZenWeaverPredictor : IEventPredictor
{
    public Type EventType => typeof(ZenWeaver);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int gold = owner.Gold;
        int breathingCost = ReadIntVar(eventModel, "BreathingTechniquesCost", 50);
        int emotionalCost = ReadIntVar(eventModel, "EmotionalAwarenessCost", 125);
        int arachnidCost = ReadIntVar(eventModel, "ArachnidAcupunctureCost", 250);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current gold", "当前金币"),
                gold.ToString(),
                PredictionTag.Normal),
            BuildOptionRow(
                STS2AdvisorI18n.Pick("Breathing Techniques", "呼吸法"),
                breathingCost,
                gold,
                STS2AdvisorI18n.Pick("Pay gold and add 2 Enlightenment cards to deck.", "支付金币并向牌组加入 2 张 Enlightenment。")),
            BuildOptionRow(
                STS2AdvisorI18n.Pick("Emotional Awareness", "情绪觉察"),
                emotionalCost,
                gold,
                STS2AdvisorI18n.Pick("Remove 1 chosen card from deck, then pay gold.", "先从牌组移除 1 张自选卡牌，再支付金币。")),
            BuildOptionRow(
                STS2AdvisorI18n.Pick("Arachnid Acupuncture", "蛛针疗法"),
                arachnidCost,
                gold,
                STS2AdvisorI18n.Pick("Remove 2 chosen cards from deck, then pay gold.", "先从牌组移除 2 张自选卡牌，再支付金币。"))
        };
    }

    private static EventPrediction BuildOptionRow(string label, int cost, int gold, string effect)
    {
        bool available = gold >= cost;
        string status = available
            ? STS2AdvisorI18n.Pick($"Cost {cost} (available). ", $"花费 {cost}（可选）。")
            : STS2AdvisorI18n.Pick($"Cost {cost} (locked). ", $"花费 {cost}（锁定）。");
        return new EventPrediction(
            label,
            status + effect,
            available ? PredictionTag.Warning : PredictionTag.Normal);
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;
        return eventModel.DynamicVars[key].IntValue;
    }
}
