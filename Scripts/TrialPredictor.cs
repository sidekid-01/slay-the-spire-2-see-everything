using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class TrialPredictor : IEventPredictor
{
    public Type EventType => typeof(Trial);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int branch = mirrorRng.NextInt(3);
        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Reject path", "拒绝分支"),
                STS2AdvisorI18n.Pick(
                    "You can re-accept, or choose Double Down (opens abandon run confirmation).",
                    "你可以再次接受，或选择 Double Down（会弹出弃局确认）。"),
                PredictionTag.Bad)
        };

        switch (branch)
        {
            case 0:
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Accept branch roll", "接受分支抽取"),
                    STS2AdvisorI18n.Pick("Merchant trial", "商人审判"),
                    PredictionTag.Normal));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Merchant - Guilty", "商人 - 有罪"),
                    STS2AdvisorI18n.Pick(
                        "Add Regret curse, then obtain next 2 relics from relic queue front.",
                        "加入 Regret 诅咒，然后获得遗物队列前端接下来的 2 个遗物。"),
                    PredictionTag.Warning));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Merchant - Innocent", "商人 - 无罪"),
                    STS2AdvisorI18n.Pick(
                        "Add Shame curse, then choose 2 cards to upgrade.",
                        "加入 Shame 诅咒，然后选择 2 张牌升级。"),
                    PredictionTag.Warning));
                break;
            case 1:
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Accept branch roll", "接受分支抽取"),
                    STS2AdvisorI18n.Pick("Noble trial", "贵族审判"),
                    PredictionTag.Normal));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Noble - Guilty", "贵族 - 有罪"),
                    STS2AdvisorI18n.Pick(
                        "Heal 10 HP.",
                        "回复 10 点生命。"),
                    PredictionTag.Good));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Noble - Innocent", "贵族 - 无罪"),
                    STS2AdvisorI18n.Pick(
                        "Add Regret curse, then gain 300 gold.",
                        "加入 Regret 诅咒，然后获得 300 金币。"),
                    PredictionTag.Warning));
                break;
            default:
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Accept branch roll", "接受分支抽取"),
                    STS2AdvisorI18n.Pick("Nondescript trial", "路人审判"),
                    PredictionTag.Normal));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Nondescript - Guilty", "路人 - 有罪"),
                    STS2AdvisorI18n.Pick(
                        "Add Doubt curse, then gain 2 card rewards (each 3 options).",
                        "加入 Doubt 诅咒，然后获得 2 组卡牌奖励（每组 3 选）。"),
                    PredictionTag.Warning));
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick("Nondescript - Innocent", "路人 - 无罪"),
                    STS2AdvisorI18n.Pick(
                        "Add Doubt curse, then transform 2 chosen cards (uses Niche RNG).",
                        "加入 Doubt 诅咒，然后变形 2 张自选卡牌（使用 Niche RNG）。"),
                    PredictionTag.Warning));
                break;
        }

        return rows;
    }
}
