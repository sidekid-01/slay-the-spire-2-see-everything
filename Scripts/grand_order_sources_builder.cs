using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace STS2Advisor.Scripts;

/// <summary>
/// Build GrandOrderNetSnapshot(Scene/Option/Description) for non-Event choice screens.
/// Keep it lightweight: we only surface what our text-based UI can reliably show.
/// </summary>
internal static class GrandOrderChoiceScreensBuilder
{
    private static readonly Regex BbCodePattern = new Regex("\\[[^\\]]+\\]", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new Regex("\\s+", RegexOptions.Compiled);

    internal static GrandOrderNetSnapshot BuildRewardsScreen(NRewardsScreen rewardsScreen)
    {
        List<Reward> rewards = ExtractRewards(rewardsScreen);
        if (rewards.Count == 0)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        string scene = STS2AdvisorI18n.Pick("Rewards", "奖励");
        var optionLines = new List<string>();
        foreach (Reward reward in rewards)
        {
            string tag = GetRewardTag(reward);
            string title = BuildRewardTitle(reward, tag);
            string desc = Sanitize(GrandOrderEventChoicesBuilder.ResolveLocString(reward.Description));

            string line = string.IsNullOrWhiteSpace(desc)
                ? (string.IsNullOrWhiteSpace(title) ? tag : title)
                : (string.IsNullOrWhiteSpace(title) ? desc : $"{title} - {desc}");

            if (!string.IsNullOrWhiteSpace(line))
                optionLines.Add(line);
        }

        string optionText = string.Join("\n", optionLines);
        string description = STS2AdvisorI18n.Pick(
            $"Reward options: {rewards.Count}",
            $"奖励选项：{rewards.Count}");

        return new GrandOrderNetSnapshot
        {
            Scene = scene,
            Option = optionText,
            Description = description,
        };
    }

    internal static GrandOrderNetSnapshot BuildCardRewardSelection(
        IReadOnlyList<CardCreationResult> options,
        IReadOnlyList<CardRewardAlternative> extraOptions)
    {
        int optionCount = options?.Count ?? 0;
        int extraCount = extraOptions?.Count ?? 0;
        if (optionCount == 0 && extraCount == 0)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        string scene = STS2AdvisorI18n.Pick("Card Reward", "选牌");
        var lines = new List<string>();

        foreach (CardCreationResult option in options ?? Array.Empty<CardCreationResult>())
        {
            var card = option.Card;
            if (card == null) continue;

            string title = Sanitize(LocText.Of(card));
            string desc = Sanitize(ResolveModelDescription(card));
            string line = string.IsNullOrWhiteSpace(desc) ? title : $"{title} - {desc}";
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        foreach (CardRewardAlternative extra in extraOptions ?? Array.Empty<CardRewardAlternative>())
        {
            string title = Sanitize(GrandOrderEventChoicesBuilder.ResolveLocString(extra.Title));
            if (!string.IsNullOrWhiteSpace(extra.Hotkey))
                title = string.IsNullOrWhiteSpace(title) ? extra.Hotkey : $"{title} ({extra.Hotkey})";

            if (!string.IsNullOrWhiteSpace(title))
                lines.Add(title);
        }

        return new GrandOrderNetSnapshot
        {
            Scene = scene,
            Option = string.Join("\n", lines),
            Description = STS2AdvisorI18n.Pick(
                $"Card choices: {optionCount}",
                $"选牌数量：{optionCount}"),
        };
    }

    internal static GrandOrderNetSnapshot BuildRelicSelection(IReadOnlyList<RelicModel> relics)
    {
        if (relics == null || relics.Count == 0)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        string scene = STS2AdvisorI18n.Pick("Relic Selection", "选遗物");
        var lines = new List<string>();
        foreach (RelicModel relic in relics)
        {
            if (relic == null) continue;
            string title = Sanitize(LocText.Of(relic));
            string desc = Sanitize(ResolveModelDescription(relic));
            string line = string.IsNullOrWhiteSpace(desc) ? title : $"{title} - {desc}";
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        return new GrandOrderNetSnapshot
        {
            Scene = scene,
            Option = string.Join("\n", lines),
            Description = STS2AdvisorI18n.Pick(
                $"Relic options: {relics.Count}",
                $"遗物数量：{relics.Count}"),
        };
    }

    internal static GrandOrderNetSnapshot BuildMerchantInventory(NMerchantInventory merchantInventory)
    {
        if (merchantInventory == null || !merchantInventory.IsOpen || merchantInventory.Inventory == null)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        MerchantInventory inv = merchantInventory.Inventory;
        var lines = new List<string>();

        lines.AddRange(inv.CharacterCardEntries
            .Where(e => e.CreationResult != null)
            .SelectMany(e => BuildMerchantCardLine(e.CreationResult!)));

        lines.AddRange(inv.ColorlessCardEntries
            .Where(e => e.CreationResult != null)
            .SelectMany(e => BuildMerchantCardLine(e.CreationResult!)));

        lines.AddRange(inv.RelicEntries
            .Where(e => e.Model != null)
            .Select(e => BuildMerchantRelicLine(e.Model!)));

        lines.AddRange(inv.PotionEntries
            .Where(e => e.Model != null)
            .Select(e => BuildMerchantPotionLine(e.Model!)));

        if (inv.CardRemovalEntry != null)
        {
            lines.Add(BuildMerchantCardRemovalLine(inv.CardRemovalEntry));
        }

        lines = lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (lines.Count == 0)
        {
            return new GrandOrderNetSnapshot
            {
                Scene = "",
                Option = "",
                Description = "",
            };
        }

        string scene = STS2AdvisorI18n.Pick("Merchant", "商店");
        string description = STS2AdvisorI18n.Pick(
            $"Shop items: {lines.Count} (Gold: {inv.Player.Gold})",
            $"商店物品：{lines.Count}（金币：{inv.Player.Gold}）");

        return new GrandOrderNetSnapshot
        {
            Scene = scene,
            Option = string.Join("\n", lines),
            Description = description,
        };
    }

    private static IEnumerable<string> BuildMerchantCardLine(CardCreationResult cardResult)
    {
        if (cardResult.Card == null)
            yield break;

        CardModel card = cardResult.Card;
        string title = Sanitize(LocText.Of(card));
        string desc = Sanitize(ResolveModelDescription(card));
        if (string.IsNullOrWhiteSpace(desc))
            yield return title;
        else
            yield return $"{title} - {desc}";
    }

    private static string BuildMerchantRelicLine(RelicModel relic)
    {
        string title = Sanitize(LocText.Of(relic));
        string desc = Sanitize(ResolveModelDescription(relic));
        return string.IsNullOrWhiteSpace(desc) ? title : $"{title} - {desc}";
    }

    private static string BuildMerchantPotionLine(PotionModel potion)
    {
        string title = Sanitize(LocText.Of(potion));
        string desc = Sanitize(ResolveModelDescription(potion));
        return string.IsNullOrWhiteSpace(desc) ? title : $"{title} - {desc}";
    }

    private static string BuildMerchantCardRemovalLine(MerchantCardRemovalEntry entry)
    {
        string title = STS2AdvisorI18n.Pick("Card Removal", "移除卡牌");
        string state = entry.Used
            ? STS2AdvisorI18n.Pick("Used", "已使用")
            : STS2AdvisorI18n.Pick("Available", "可用");

        // EnoughGold exists on MerchantEntry-derived types; keep it best-effort.
        bool enoughGold = TryGetBoolProperty(entry, "EnoughGold", out var v) && v;
        bool disabled = !enoughGold;
        string lockText = disabled ? STS2AdvisorI18n.Pick("Locked", "不可用") : string.Empty;

        return string.IsNullOrWhiteSpace(lockText)
            ? $"{title} - {state}"
            : $"{title} - {state} ({lockText})";
    }

    private static bool TryGetBoolProperty(object? obj, string propName, out bool value)
    {
        value = false;
        if (obj == null) return false;
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(obj) is bool b)
            {
                value = b;
                return true;
            }
        }
        catch
        {
            // Ignore reflection failures
        }
        return false;
    }

    private static List<Reward> ExtractRewards(NRewardsScreen rewardsScreen)
    {
        var list = new List<Reward>();
        Control? container = ((Node)rewardsScreen).GetNodeOrNull<Control>("%RewardsContainer");
        if (container == null)
            return list;

        foreach (Node child in container.GetChildren(false))
        {
            if (child is NRewardButton btn && btn.Reward != null)
            {
                list.Add(btn.Reward);
                continue;
            }

            if (child is NLinkedRewardSet set && set.LinkedRewardSet != null)
            {
                list.AddRange(set.LinkedRewardSet.Rewards);
            }
        }

        return list;
    }

    private static string GetRewardTag(Reward reward)
    {
        return reward switch
        {
            RelicReward => STS2AdvisorI18n.Pick("Relic", "遗物"),
            PotionReward => STS2AdvisorI18n.Pick("Potion", "药水"),
            CardReward => STS2AdvisorI18n.Pick("Card", "牌"),
            GoldReward => STS2AdvisorI18n.Pick("Gold", "金币"),
            CardRemovalReward => STS2AdvisorI18n.Pick("Action", "行动"),
            _ => STS2AdvisorI18n.Pick("Reward", "奖励"),
        };
    }

    private static string BuildRewardTitle(Reward reward, string fallbackTag)
    {
        // Best effort: use LocText.Of when models exist; otherwise fall back to tag.
        try
        {
            if (reward is RelicReward relicReward)
            {
                var relic = TryExtractPrivateField<RelicModel>(relicReward, "_relic");
                if (relic != null) return Sanitize(LocText.Of(relic));
            }
            else if (reward is PotionReward potionReward)
            {
                // PotionReward.Potion is public in STS2.
                if (potionReward.Potion != null) return Sanitize(LocText.Of(potionReward.Potion));
            }
            else if (reward is CardReward cardReward)
            {
                // CardReward has private List<CardCreationResult> _cards.
                var cards = TryExtractPrivateField<List<CardCreationResult>>(cardReward, "_cards");
                var first = cards?.FirstOrDefault();
                if (first?.Card != null)
                {
                    string title = Sanitize(LocText.Of(first.Card));
                    return string.IsNullOrWhiteSpace(title) ? fallbackTag : title;
                }
            }
        }
        catch
        {
            // ignore and fallback
        }

        return fallbackTag;
    }

    private static T? TryExtractPrivateField<T>(object obj, string fieldName) where T : class
    {
        try
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(obj) as T;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveModelDescription(object model)
    {
        // STS2 models usually expose LocString Description / DescriptionLocString.
        foreach (string propName in new[] { "Description", "DescriptionLocString" })
        {
            var ls = TryGetLocStringProperty(model, propName);
            if (ls != null && !LocString.IsNullOrWhitespace(ls))
                return ls.GetFormattedTextSafe();
        }

        return string.Empty;
    }

    private static LocString? TryGetLocStringProperty(object model, string propName)
    {
        try
        {
            var prop = model.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(model) is LocString ls)
                return ls;
        }
        catch
        {
            // ignore
        }
        return null;
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

internal static class GrandOrderLocStringExtensions
{
    internal static string GetFormattedTextSafe(this LocString loc)
    {
        try
        {
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
}

