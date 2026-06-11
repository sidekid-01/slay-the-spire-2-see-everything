using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace STS2Advisor.Scripts;

public class TabletOfTruthPredictor : IEventPredictor
{
    public Type EventType => typeof(TabletOfTruth);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int decipherCount = ReadDecipherCount(eventModel);
        int currentCost = eventModel.DynamicVars["DecipherMaxHpLoss"].IntValue;
        int smashHeal = eventModel.DynamicVars["SmashHPGain"].IntValue;

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Current decipher step", "当前解读阶段"),
                STS2AdvisorI18n.Pick($"{decipherCount + 1}/5", $"第 {decipherCount + 1}/5 次"),
                PredictionTag.Normal),
            new(
                STS2AdvisorI18n.Pick("Smash", "砸碎"),
                STS2AdvisorI18n.Pick(
                    $"Heal {smashHeal} HP and end event.",
                    $"回复 {smashHeal} 生命并结束事件。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Decipher (current)", "解读（当前）"),
                DescribeCurrentDecipher(owner.Creature.MaxHp, currentCost, decipherCount),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Decipher costs", "解读代价轨迹"),
                STS2AdvisorI18n.Pick(
                    "Step costs: 3 -> 6 -> 12 -> 24 -> (MaxHP-1).",
                    "阶段代价：3 -> 6 -> 12 -> 24 ->（最大生命-1）。"),
                PredictionTag.Normal)
        };

        return rows;
    }

    private static string DescribeCurrentDecipher(int maxHp, int currentCost, int decipherCount)
    {
        bool lethal = maxHp <= currentCost;
        string lethalPart = lethal
            ? STS2AdvisorI18n.Pick("Current max HP is too low: choosing this kills you.", "当前最大生命不足：选择此项会死亡。")
            : STS2AdvisorI18n.Pick("This choice is survivable.", "当前选择可存活。");

        string rewardPart = decipherCount >= 4
            ? STS2AdvisorI18n.Pick("Upgrades all upgradable deck cards.", "会升级牌组中所有可升级卡牌。")
            : STS2AdvisorI18n.Pick("Upgrades 1 random upgradable deck card.", "会随机升级 1 张可升级牌组卡牌。");

        return STS2AdvisorI18n.Pick(
            $"Lose {currentCost} Max HP. {rewardPart} {lethalPart}",
            $"失去 {currentCost} 点最大生命。{rewardPart} {lethalPart}");
    }

    private static int ReadDecipherCount(EventModel eventModel)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var field = eventModel.GetType().GetField("_decipherCount", flags);
        if (field?.GetValue(eventModel) is int n)
            return n;
        return 0;
    }
}
