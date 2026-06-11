using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class ReflectionsPredictor : IEventPredictor
{
    public Type EventType => typeof(Reflections);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var rows = new List<EventPrediction>();

        var upgraded = owner.Deck.Cards.Where(c => c.IsUpgraded).ToList();
        var downgradedNames = RollDistinctNames(upgraded, take: 2, mirrorRng);

        var upgradable = owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        var upgradedNames = RollDistinctNames(upgradable, take: 4, mirrorRng);

        string touchText = STS2AdvisorI18n.Pick(
            "Downgrade up to 2 random upgraded cards, then upgrade up to 4 random upgradable cards.",
            "随机降级至多 2 张已升级卡，然后随机升级至多 4 张可升级卡。");
        touchText += " " + STS2AdvisorI18n.Pick(
            $"Downgrade: {FormatNames(downgradedNames)}; Upgrade: {FormatNames(upgradedNames)}.",
            $"降级：{FormatNames(downgradedNames)}；升级：{FormatNames(upgradedNames)}。");

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Touch a Mirror", "触碰镜子"),
            touchText,
            PredictionTag.Warning));

        int originalDeckSize = owner.Deck.Cards.Count;
        string shatterText = STS2AdvisorI18n.Pick(
            $"Duplicate all deck cards (+{originalDeckSize}), then add Bad Luck curse.",
            $"复制整副牌（+{originalDeckSize} 张），然后加入 1 张 Bad Luck 诅咒。");
        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Shatter", "击碎"),
            shatterText,
            PredictionTag.Bad));

        return rows;
    }

    private static List<string> RollDistinctNames(List<CardModel> candidates, int take, Rng rng)
    {
        var pool = new List<CardModel>(candidates);
        var names = new List<string>();
        for (int i = 0; i < take; i++)
        {
            if (pool.Count == 0) break;
            int idx = rng.NextInt(0, pool.Count);
            var picked = pool[idx];
            pool.RemoveAt(idx);
            names.Add(LocText.Of(picked));
        }

        return names;
    }

    private static string FormatNames(List<string> names)
    {
        if (names.Count == 0)
            return STS2AdvisorI18n.Pick("none", "无");
        return string.Join(" / ", names);
    }
}
