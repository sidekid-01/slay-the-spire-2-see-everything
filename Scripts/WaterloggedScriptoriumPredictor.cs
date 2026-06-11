using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class WaterloggedScriptoriumPredictor : IEventPredictor
{
    public Type EventType => typeof(WaterloggedScriptorium);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int currentGold = owner.Gold;
        int tentacleCost = eventModel.DynamicVars.Gold.IntValue;
        int pricklyCost = ReadIntVar(eventModel, "PricklySpongeGold", 0);
        int pricklyCards = eventModel.DynamicVars.Cards.IntValue;
        int maxHpGain = eventModel.DynamicVars.MaxHp.IntValue;

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current gold", "当前金币"),
                currentGold.ToString(),
                PredictionTag.Normal),
            new(
                STS2AdvisorI18n.Pick("Bloody Ink", "血墨"),
                STS2AdvisorI18n.Pick(
                    $"Gain +{maxHpGain} Max HP.",
                    $"获得 +{maxHpGain} 最大生命。"),
                PredictionTag.Good),
            BuildGoldOption(
                STS2AdvisorI18n.Pick("Tentacle Quill", "触须羽笔"),
                currentGold,
                tentacleCost,
                STS2AdvisorI18n.Pick(
                    "Pay gold, then choose 1 card to gain Steady +1.",
                    "支付金币，然后选择 1 张牌获得 沉着 +1。")),
            BuildGoldOption(
                STS2AdvisorI18n.Pick("Prickly Sponge", "刺海绵"),
                currentGold,
                pricklyCost,
                STS2AdvisorI18n.Pick(
                    $"Pay gold, then choose up to {pricklyCards} cards to gain Steady +1.",
                    $"支付金币，然后选择至多 {pricklyCards} 张牌获得 沉着 +1。"))
        };

        return rows;
    }

    private static EventPrediction BuildGoldOption(string label, int currentGold, int cost, string effect)
    {
        bool affordable = currentGold >= cost;
        string status = affordable
            ? STS2AdvisorI18n.Pick($"Cost {cost} (affordable). ", $"花费 {cost}（可选）。")
            : STS2AdvisorI18n.Pick($"Cost {cost} (locked). ", $"花费 {cost}（锁定）。");
        return new EventPrediction(
            label,
            status + effect,
            affordable ? PredictionTag.Warning : PredictionTag.Normal);
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;
        return eventModel.DynamicVars[key].IntValue;
    }
}
