using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class TeaMasterPredictor : IEventPredictor
{
    public Type EventType => typeof(TeaMaster);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int gold = owner.Gold;
        int boneCost = ReadIntVar(eventModel, "BoneTeaCost", 0);
        int emberCost = ReadIntVar(eventModel, "EmberTeaCost", 0);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current gold", "当前金币"),
                gold.ToString(),
                PredictionTag.Normal),
            BuildPaidTeaRow(
                STS2AdvisorI18n.Pick("Bone Tea", "骨茶"),
                boneCost,
                gold,
                STS2AdvisorI18n.Pick("Obtain BoneTea relic.", "获得 BoneTea 遗物。")),
            BuildPaidTeaRow(
                STS2AdvisorI18n.Pick("Ember Tea", "余烬茶"),
                emberCost,
                gold,
                STS2AdvisorI18n.Pick("Obtain EmberTea relic.", "获得 EmberTea 遗物。")),
            new(
                STS2AdvisorI18n.Pick("Tea of Discourtesy", "失礼之茶"),
                STS2AdvisorI18n.Pick("No gold cost, obtain TeaOfDiscourtesy relic.", "无需金币，获得 TeaOfDiscourtesy 遗物。"),
                PredictionTag.Warning)
        };
    }

    private static EventPrediction BuildPaidTeaRow(string label, int cost, int currentGold, string effect)
    {
        bool affordable = currentGold >= cost;
        string status = affordable
            ? STS2AdvisorI18n.Pick($"Cost {cost} (available). ", $"花费 {cost}（可选）。")
            : STS2AdvisorI18n.Pick($"Cost {cost} (locked). ", $"花费 {cost}（锁定）。");

        return new EventPrediction(
            label,
            status + effect,
            affordable ? PredictionTag.Good : PredictionTag.Normal);
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;
        return eventModel.DynamicVars[key].IntValue;
    }
}
