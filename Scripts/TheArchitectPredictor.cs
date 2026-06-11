using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace STS2Advisor.Scripts;

public class TheArchitectPredictor : IEventPredictor
{
    public Type EventType => typeof(TheArchitect);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var rows = new List<EventPrediction>();
        object? dialogue = ReadMember(eventModel, "_dialogue");
        if (dialogue == null)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Dialogue", "对话"),
                STS2AdvisorI18n.Pick("No dialogue loaded; event will show proceed option.", "未载入对话；事件将显示继续选项。"),
                PredictionTag.Normal));
            return rows;
        }

        int lineCount = ReadLineCount(dialogue);
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Dialogue lines", "对话行数"),
            lineCount.ToString(),
            PredictionTag.Normal));

        string startAttackers = ReadEnumName(dialogue, "StartAttackers");
        string endAttackers = ReadEnumName(dialogue, "EndAttackers");
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Opening animation", "开场演出"),
            string.IsNullOrWhiteSpace(startAttackers)
                ? STS2AdvisorI18n.Pick("No attack animation.", "无攻击演出。")
                : STS2AdvisorI18n.Pick($"Start attackers: {startAttackers}", $"开场攻击方：{startAttackers}"),
            PredictionTag.Warning));
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Ending animation", "结尾演出"),
            string.IsNullOrWhiteSpace(endAttackers)
                ? STS2AdvisorI18n.Pick("No attack animation.", "无攻击演出。")
                : STS2AdvisorI18n.Pick($"End attackers: {endAttackers}", $"结尾攻击方：{endAttackers}"),
            PredictionTag.Warning));

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Flow", "流程"),
            STS2AdvisorI18n.Pick("Advance through dialogue, then proceed to end run flow.", "逐行推进对话，最后进入继续流程。"),
            PredictionTag.Normal));

        return rows;
    }

    private static object? ReadMember(object instance, string name)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var f = instance.GetType().GetField(name, Flags);
        if (f != null) return f.GetValue(instance);
        var p = instance.GetType().GetProperty(name, Flags);
        if (p != null) return p.GetValue(instance);
        return null;
    }

    private static int ReadLineCount(object dialogue)
    {
        object? lines = ReadMember(dialogue, "Lines");
        if (lines is System.Collections.ICollection coll)
            return coll.Count;
        return 0;
    }

    private static string ReadEnumName(object dialogue, string member)
    {
        object? value = ReadMember(dialogue, member);
        return value?.ToString() ?? string.Empty;
    }
}
