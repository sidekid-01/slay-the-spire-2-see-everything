using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class RelicTraderPredictor : IEventPredictor
{
    public Type EventType => typeof(RelicTrader);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var rows = new List<EventPrediction>();

        string topOwned = ReadStringVar(eventModel, "TopRelicOwned");
        string topNew = ReadStringVar(eventModel, "TopRelicNew");
        AddTradeRow(rows, 1, topOwned, topNew);

        string midOwned = ReadStringVar(eventModel, "MiddleRelicOwned");
        string midNew = ReadStringVar(eventModel, "MiddleRelicNew");
        AddTradeRow(rows, 2, midOwned, midNew);

        string bottomOwned = ReadStringVar(eventModel, "BottomRelicOwned");
        string bottomNew = ReadStringVar(eventModel, "BottomRelicNew");
        AddTradeRow(rows, 3, bottomOwned, bottomNew);

        if (rows.Count == 0)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Trade", "交易"),
                STS2AdvisorI18n.Pick("No tradable relic options available.", "没有可交易遗物选项。"),
                PredictionTag.Normal));
        }

        return rows;
    }

    private static void AddTradeRow(List<EventPrediction> rows, int slot, string owned, string incoming)
    {
        if (string.IsNullOrWhiteSpace(owned) || string.IsNullOrWhiteSpace(incoming))
            return;

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick($"Trade option {slot}", $"交易选项 {slot}"),
            STS2AdvisorI18n.Pick($"Swap {owned} -> {incoming}", $"替换 {owned} -> {incoming}"),
            PredictionTag.Warning));
    }

    private static string ReadStringVar(EventModel eventModel, string key)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return string.Empty;

        if (eventModel.DynamicVars[key] is StringVar sv)
            return sv.StringValue ?? string.Empty;

        return string.Empty;
    }
}
