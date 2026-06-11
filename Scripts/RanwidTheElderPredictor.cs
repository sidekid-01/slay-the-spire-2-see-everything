using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class RanwidTheElderPredictor : IEventPredictor
{
    public Type EventType => typeof(RanwidTheElder);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var rows = new List<EventPrediction>();

        var potions = owner.Potions.ToList();
        string potionName = STS2AdvisorI18n.Pick("None", "无");
        if (potions.Count > 0)
        {
            int potionIndex = mirrorRng.NextInt(0, potions.Count);
            potionName = LocText.Of(potions[potionIndex]);
        }

        int goldCost = ReadIntVar(eventModel, "Gold", 100);

        var tradableRelics = owner.Relics.Where(r => r.IsTradable).ToList();
        string relicName = STS2AdvisorI18n.Pick("None", "无");
        if (tradableRelics.Count > 0)
        {
            int relicIndex = mirrorRng.NextInt(0, tradableRelics.Count);
            relicName = LocText.Of(tradableRelics[relicIndex]);
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Potion deal", "药水交易"),
            STS2AdvisorI18n.Pick(
                $"Discard potion: {potionName}; obtain next relic from relic queue front.",
                $"丢弃药水：{potionName}；获得遗物队列前端下一个遗物。"),
            PredictionTag.Warning));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Gold deal", "金币交易"),
            STS2AdvisorI18n.Pick(
                $"Spend {goldCost} gold; obtain next relic from relic queue front.",
                $"花费 {goldCost} 金币；获得遗物队列前端下一个遗物。"),
            PredictionTag.Warning));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Relic deal", "遗物交易"),
            STS2AdvisorI18n.Pick(
                $"Remove tradable relic: {relicName}; obtain next 2 relics from relic queue front.",
                $"移除可交易遗物：{relicName}；获得遗物队列前端接下来的 2 个遗物。"),
            PredictionTag.Good));

        return rows;
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;

        if (eventModel.DynamicVars[key] is DynamicVar dv)
            return dv.IntValue;

        return fallback;
    }
}
