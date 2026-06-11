using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class TinkerTimePredictor : IEventPredictor
{
    public Type EventType => typeof(TinkerTime);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var typePool = new List<CardType> { CardType.Attack, CardType.Skill, CardType.Power };
        var shownTypes = TakeRandomDistinct(typePool, 2, mirrorRng);

        // After ChooseCardType has rolled once, Rider choices start from this counter.
        int riderCounter = mirrorRng.Counter;
        uint riderSeed = mirrorRng.Seed;

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Step 1 options", "第 1 步选项"),
                string.Join(" / ", shownTypes.Select(TypeName)),
                PredictionTag.Normal)
        };

        foreach (var t in shownTypes)
        {
            var riderPool = RiderPoolFor(t);
            var peek = new Rng(riderSeed, riderCounter);
            var shownRiders = TakeRandomDistinct(riderPool, 2, peek);
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"If choose {TypeName(t)}", $"若选择 {TypeName(t)}"),
                STS2AdvisorI18n.Pick("Rider options: ", "Rider 选项：")
                    + string.Join(" / ", shownRiders.Select(RiderName)),
                PredictionTag.Warning));
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Final reward", "最终奖励"),
            STS2AdvisorI18n.Pick(
                "Add 1 MadScience card to deck with selected type + rider.",
                "向牌组加入 1 张 MadScience（携带你选定的类型与 Rider）。"),
            PredictionTag.Good));

        return rows;
    }

    private static List<T> TakeRandomDistinct<T>(List<T> source, int count, Rng rng)
    {
        var pool = source.ToList();
        var result = new List<T>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = rng.NextInt(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    private static List<TinkerTime.RiderEffect> RiderPoolFor(CardType t) => t switch
    {
        CardType.Attack => new() { TinkerTime.RiderEffect.Sapping, TinkerTime.RiderEffect.Violence, TinkerTime.RiderEffect.Choking },
        CardType.Skill => new() { TinkerTime.RiderEffect.Energized, TinkerTime.RiderEffect.Wisdom, TinkerTime.RiderEffect.Chaos },
        CardType.Power => new() { TinkerTime.RiderEffect.Expertise, TinkerTime.RiderEffect.Curious, TinkerTime.RiderEffect.Improvement },
        _ => new()
    };

    private static string TypeName(CardType t) => t switch
    {
        CardType.Attack => STS2AdvisorI18n.Pick("Attack", "攻击"),
        CardType.Skill => STS2AdvisorI18n.Pick("Skill", "技能"),
        CardType.Power => STS2AdvisorI18n.Pick("Power", "能力"),
        _ => t.ToString()
    };

    private static string RiderName(TinkerTime.RiderEffect rider) => rider switch
    {
        TinkerTime.RiderEffect.Sapping => STS2AdvisorI18n.Pick("Sapping", "削弱"),
        TinkerTime.RiderEffect.Violence => STS2AdvisorI18n.Pick("Violence", "暴烈"),
        TinkerTime.RiderEffect.Choking => STS2AdvisorI18n.Pick("Choking", "窒息"),
        TinkerTime.RiderEffect.Energized => STS2AdvisorI18n.Pick("Energized", "充能"),
        TinkerTime.RiderEffect.Wisdom => STS2AdvisorI18n.Pick("Wisdom", "智慧"),
        TinkerTime.RiderEffect.Chaos => STS2AdvisorI18n.Pick("Chaos", "混沌"),
        TinkerTime.RiderEffect.Expertise => STS2AdvisorI18n.Pick("Expertise", "专精"),
        TinkerTime.RiderEffect.Curious => STS2AdvisorI18n.Pick("Curious", "好奇"),
        TinkerTime.RiderEffect.Improvement => STS2AdvisorI18n.Pick("Improvement", "改进"),
        _ => rider.ToString()
    };
}
