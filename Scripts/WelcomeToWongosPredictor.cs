using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class WelcomeToWongosPredictor : IEventPredictor
{
    public Type EventType => typeof(WelcomeToWongos);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int gold = owner.Gold;
        int bargainCost = ReadIntVar(eventModel, "BargainBinCost", 0);
        int featuredCost = ReadIntVar(eventModel, "FeaturedItemCost", 0);
        int mysteryCost = ReadIntVar(eventModel, "MysteryBoxCost", 0);
        string featuredRelic = ReadStringVar(eventModel, "RandomRelic");
        int currentWongoPoints = SaveManager.Instance.Progress.WongoPoints;
        int cyclePoints = currentWongoPoints % 2000;
        int badgeTotal = currentWongoPoints / 2000;

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current gold", "当前金币"),
                gold.ToString(),
                PredictionTag.Normal),
            new(
                STS2AdvisorI18n.Pick("Featured rare relic", "本期稀有遗物"),
                string.IsNullOrWhiteSpace(featuredRelic)
                    ? STS2AdvisorI18n.Pick("Unavailable", "未读取到")
                    : featuredRelic,
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Wongo points", "Wongo 积分"),
                STS2AdvisorI18n.Pick(
                    $"Current cycle points: {cyclePoints}/2000",
                    $"当前周期积分：{cyclePoints}/2000"),
                PredictionTag.Normal),
            new(
                STS2AdvisorI18n.Pick("Wongo badges obtained", "已获 Wongo 徽章"),
                badgeTotal.ToString(),
                PredictionTag.Normal),
            BuildCostRow(
                STS2AdvisorI18n.Pick("Bargain Bin", "折扣区"),
                bargainCost,
                gold,
                STS2AdvisorI18n.Pick(
                    "Buy: obtain next Common relic from relic queue front.",
                    "购买后：获得遗物队列前端的下一个普通遗物。"),
                BuildBadgeHint(cyclePoints, pointsEarned: 32)),
            BuildCostRow(
                STS2AdvisorI18n.Pick("Featured Item", "本期特选"),
                featuredCost,
                gold,
                STS2AdvisorI18n.Pick(
                    "Buy: obtain featured Rare relic shown above.",
                    "购买后：获得上方展示的本期稀有遗物。"),
                BuildBadgeHint(cyclePoints, pointsEarned: 16)),
            BuildCostRow(
                STS2AdvisorI18n.Pick("Mystery Box", "神秘盲盒"),
                mysteryCost,
                gold,
                STS2AdvisorI18n.Pick(
                    "Buy: obtain Wongos Mystery Ticket (after 5 combats, add 3 relic rewards once).",
                    "购买后：获得 Wongo 神秘票（5 场战斗后，一次性额外加入 3 个遗物奖励）。"),
                BuildBadgeHint(cyclePoints, pointsEarned: 8)),
            new(
                STS2AdvisorI18n.Pick("Leave", "离开"),
                STS2AdvisorI18n.Pick("Always available.", "始终可选。"),
                PredictionTag.Normal)
        };

        return rows;
    }

    private static EventPrediction BuildCostRow(string label, int cost, int gold, string onBuyEffect, string badgeHint)
    {
        bool affordable = gold >= cost;
        string status = affordable
            ? STS2AdvisorI18n.Pick($"Cost {cost} (affordable). ", $"花费 {cost}（可购买）。")
            : STS2AdvisorI18n.Pick($"Cost {cost} (locked). ", $"花费 {cost}（不可购买）。");
        string value = status + onBuyEffect + " " + badgeHint;
        return new EventPrediction(label, value, affordable ? PredictionTag.Good : PredictionTag.Bad);
    }

    private static string BuildBadgeHint(int cyclePoints, int pointsEarned)
    {
        int after = cyclePoints + pointsEarned;
        int remaining = Math.Max(2000 - after, 0);
        if (after >= 2000)
        {
            return STS2AdvisorI18n.Pick(
                $"Wongo points +{pointsEarned}; this purchase reaches 2000 and grants a Wongo Customer Appreciation Badge.",
                $"Wongo 积分 +{pointsEarned}；本次购买达到 2000，将获得 Wongo 顾客感谢徽章。");
        }

        return STS2AdvisorI18n.Pick(
            $"Wongo points +{pointsEarned}; cycle progress becomes {after}/2000, remaining {remaining}.",
            $"Wongo 积分 +{pointsEarned}；本轮进度将变为 {after}/2000，距离徽章还差 {remaining}。");
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;

        return eventModel.DynamicVars[key].IntValue;
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
