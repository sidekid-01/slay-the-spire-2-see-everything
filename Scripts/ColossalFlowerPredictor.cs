using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace STS2Advisor.Scripts;

public class ColossalFlowerPredictor : IEventPredictor
{
    public Type EventType => typeof(ColossalFlower);

    private static readonly int[] PrizeGold = { 35, 75, 135 };
    private static readonly int[] PrizeDamage = { 5, 6, 7 };

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int digs = ReadCurrentDigs(eventModel);
        digs = Math.Clamp(digs, 0, 2);

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current depth", "当前深度"),
                STS2AdvisorI18n.Pick($"Dig #{digs + 1}", $"第 {digs + 1} 层"),
                PredictionTag.Normal),
            new(
                STS2AdvisorI18n.Pick("Extract current prize", "提取当前奖励"),
                STS2AdvisorI18n.Pick(
                    $"Gain {PrizeGold[digs]} gold.",
                    $"获得 {PrizeGold[digs]} 金币。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Reach deeper", "继续深入"),
                STS2AdvisorI18n.Pick(
                    $"Take {PrizeDamage[digs]} damage and move to next depth.",
                    $"受到 {PrizeDamage[digs]} 点伤害并进入下一层。"),
                PredictionTag.Warning)
        };

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Deepest branch", "最深层分支"),
            STS2AdvisorI18n.Pick(
                "At depth 3: choose either Extract Instead (135 gold) or take 7 damage to obtain Pollinous Core.",
                "到第 3 层时：可二选一，提取奖励（135 金币）或再受 7 点伤害获得 Pollinous Core。"),
            PredictionTag.Warning));

        return rows;
    }

    private static int ReadCurrentDigs(EventModel eventModel)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var field = eventModel.GetType().GetField("_numberOfDigs", flags);
        if (field?.GetValue(eventModel) is int n)
            return n;

        var prop = eventModel.GetType().GetProperty("NumberOfDigs", flags);
        if (prop?.GetValue(eventModel) is int p)
            return p;

        return 0;
    }
}
