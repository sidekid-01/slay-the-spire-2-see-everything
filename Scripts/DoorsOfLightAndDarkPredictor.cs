using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class DoorsOfLightAndDarkPredictor : IEventPredictor
{
    public Type EventType => typeof(DoorsOfLightAndDark);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var player = eventModel.Owner;
        if (player == null || player.RunState == null)
            return new();

        return new()
        {
            new(
                STS2AdvisorI18n.Pick("Light", "光明"),
                PredictLightUpgrade(player),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Dark", "黑暗"),
                STS2AdvisorI18n.Pick("Choose 1 card to remove from deck.", "从牌组中选择 1 张牌移除。"),
                PredictionTag.Warning)
        };
    }

    private static string PredictLightUpgrade(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var candidates = PileType.Deck.GetPile(player).Cards
            .Where(c => c?.IsUpgradable ?? false)
            .ToList();

        if (candidates.Count == 0)
            return STS2AdvisorI18n.Pick("No upgradable cards.", "没有可升级卡牌。");

        if (candidates.Count <= 2)
        {
            string direct = string.Join(" / ", candidates.Select(LocText.Of));
            return STS2AdvisorI18n.Pick("Will upgrade: ", "将升级：") + direct;
        }

        var niche = player.RunState.Rng.Niche;
        var peekRng = new Rng(niche.Seed, niche.Counter);
        var shuffled = StableShuffle(candidates, peekRng);
        var picked = shuffled.Take(2).Select(LocText.Of).ToList();
        return STS2AdvisorI18n.Pick("Will upgrade: ", "将升级：") + string.Join(" / ", picked);
    }

    private static List<CardModel> StableShuffle(List<CardModel> source, Rng rng)
    {
        var list = source.ToList();
        var sorted = list.ToList();
        sorted.Sort();
        for (int i = 0; i < list.Count; i++)
            list[i] = sorted[i];

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int j = rng.NextInt(n + 1);
            (list[j], list[n]) = (list[n], list[j]);
        }

        return list;
    }
}
