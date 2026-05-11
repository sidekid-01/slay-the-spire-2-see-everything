using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Events;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace STS2Advisor.Scripts;

internal static class GrandOrderEventChoicesBuilder
{
    private static readonly Regex BbCodePattern = new Regex("\\[[^\\]]+\\]", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new Regex("\\s+", RegexOptions.Compiled);

    internal static GrandOrderNetSnapshot BuildEventChoices(NEventLayout eventLayout)
    {
        var buttons = eventLayout.OptionButtons?.ToList() ?? new();
        if (buttons.Count == 0)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        var first = buttons.FirstOrDefault();
        string scene = first != null
            ? Sanitize(ResolveLocString(first.Event?.Title))
            : STS2AdvisorI18n.Pick("Event", "事件");

        var lines = buttons.Select(b =>
        {
            var opt = b.Option;
            string title = Sanitize(ResolveLocString(opt?.Title));
            string desc = Sanitize(ResolveLocString(opt?.Description));

            string status = opt switch
            {
                { IsLocked: true } => STS2AdvisorI18n.Pick("Locked", "已锁定"),
                { IsProceed: true } => STS2AdvisorI18n.Pick("Proceed", "继续"),
                _ => STS2AdvisorI18n.Pick("Choice", "选项")
            };

            // Keep one option per line; UI label wraps automatically.
            return string.IsNullOrWhiteSpace(desc)
                ? $"{title} ({status})"
                : $"{title} ({status}) - {desc}";
        }).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        string optionText = string.Join("\n", lines);
        string description = STS2AdvisorI18n.Pick(
            $"Event options available: {buttons.Count}",
            $"事件可选项：{buttons.Count}");

        return new GrandOrderNetSnapshot
        {
            Scene = scene,
            Option = optionText,
            Description = description,
        };
    }

    internal static string ResolveLocString(LocString? loc)
    {
        if (loc == null || LocString.IsNullOrWhitespace(loc))
            return string.Empty;

        try
        {
            // Try formatted text first; fall back to raw text.
            return loc.GetFormattedText();
        }
        catch
        {
            try
            {
                return loc.GetRawText();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private static string Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string input = raw.Replace('\r', ' ').Replace('\n', ' ');
        input = BbCodePattern.Replace(input, string.Empty);
        input = WhitespacePattern.Replace(input, " ");
        return input.Trim();
    }
}

