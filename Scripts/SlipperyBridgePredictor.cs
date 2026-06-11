using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class SlipperyBridgePredictor : IEventPredictor
{
    public Type EventType => typeof(SlipperyBridge);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        string randomCard = ReadStringVar(eventModel, "RandomCard");
        int hpLoss = ReadIntVar(eventModel, "HpLoss");

        var rows = new List<EventPrediction>();

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Overcome", "克服"),
            string.IsNullOrWhiteSpace(randomCard)
                ? STS2AdvisorI18n.Pick("Remove the currently highlighted random removable card.", "移除当前高亮的随机可移除卡牌。")
                : STS2AdvisorI18n.Pick("Will remove: ", "将移除：") + randomCard,
            PredictionTag.Warning));

        string holdOnText = hpLoss > 0
            ? STS2AdvisorI18n.Pick($"Take {hpLoss} damage, then reroll a new removable card.", $"受到 {hpLoss} 点伤害，然后重掷新的可移除卡牌。")
            : STS2AdvisorI18n.Pick("Take damage, then reroll a new removable card.", "受到伤害，然后重掷新的可移除卡牌。");
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Hold On", "坚持"),
            holdOnText,
            PredictionTag.Bad));

        return rows;
    }

    private static string ReadStringVar(EventModel eventModel, string key)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return string.Empty;

        if (eventModel.DynamicVars[key] is StringVar sv)
            return sv.StringValue ?? string.Empty;

        return string.Empty;
    }

    private static int ReadIntVar(EventModel eventModel, string key)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return 0;
        return eventModel.DynamicVars[key].IntValue;
    }
}
