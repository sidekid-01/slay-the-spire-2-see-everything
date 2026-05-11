using MegaCrit.Sts2.Core.Models;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace STS2Advisor.Scripts;

internal static class EventFallbackPredictor
{
    public static List<EventPrediction> Predict(EventModel eventModel)
    {
        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Prediction status", "预测状态"),
                STS2AdvisorI18n.Pick(
                    "No dedicated RNG predictor yet. Showing readable event options.",
                    "该事件暂未接入专用 RNG 预测，先显示可读事件选项。"),
                PredictionTag.Normal)
        };

        var options = ReadEventOptions(eventModel);
        if (options.Count == 0)
        {
            rows.Add(new(
                STS2AdvisorI18n.Pick("Options", "选项"),
                STS2AdvisorI18n.Pick("No option text available.", "未读取到可用选项文本。"),
                PredictionTag.Normal));
            return rows;
        }

        for (int i = 0; i < options.Count; i++)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Option {i + 1}", $"选项 {i + 1}"),
                options[i],
                PredictionTag.Normal));
        }

        return rows;
    }

    private static List<string> ReadEventOptions(EventModel eventModel)
    {
        IEnumerable<object?>? source = null;
        Type type = eventModel.GetType();

        foreach (string propertyName in new[] { "Options", "CurrentOptions", "OptionModels" })
        {
            PropertyInfo? prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(eventModel) is IEnumerable enumerable)
            {
                source = Enumerate(enumerable);
                break;
            }
        }

        if (source == null)
            return new List<string>();

        var rows = new List<string>();
        foreach (var option in source)
        {
            string text = ResolveOptionText(option);
            if (!string.IsNullOrWhiteSpace(text))
                rows.Add(text);
        }
        return rows;
    }

    private static IEnumerable<object?> Enumerate(IEnumerable enumerable)
    {
        foreach (var item in enumerable)
            yield return item;
    }

    private static string ResolveOptionText(object? option)
    {
        if (option == null)
            return string.Empty;

        string loc = LocText.Of(option);
        if (!string.IsNullOrWhiteSpace(loc))
            return loc;

        Type t = option.GetType();
        foreach (string name in new[] { "Text", "Description", "Label" })
        {
            PropertyInfo? prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object? value = prop?.GetValue(option);
            if (value == null)
                continue;

            string text = value as string ?? LocText.Of(value);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return option.ToString() ?? string.Empty;
    }
}
