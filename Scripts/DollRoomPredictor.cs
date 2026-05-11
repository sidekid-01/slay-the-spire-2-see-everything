using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class DollRoomPredictor : IEventPredictor
{
    public Type EventType => typeof(DollRoom);

    private static readonly string[] DollNamesEn = { "Daughter of Wind", "Mr. Struggles", "Tinkerbell" };
    private static readonly string[] DollNamesZh = { "风之女儿", "挣扎先生", "小叮当" };

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        int count = DollNamesEn.Length;
        int index = mirrorRng.NextInt(0, count);
        string picked = DollName(index);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Random option", "随机选项"),
                picked,
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Pay 5 HP: choose two", "扣 5 血：二选一"),
                $"{DollName((index + 1) % count)} / {DollName((index + 2) % count)}",
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pay 15 HP: take all", "扣 15 血：全拿"),
                string.Join(" / ", AllDolls()),
                PredictionTag.Bad),
        };
    }

    private static string DollName(int index) =>
        STS2AdvisorI18n.Pick(DollNamesEn[index], DollNamesZh[index]);

    private static IEnumerable<string> AllDolls()
    {
        for (int i = 0; i < DollNamesEn.Length; i++)
            yield return DollName(i);
    }
}
