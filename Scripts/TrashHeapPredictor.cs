using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class TrashHeapPredictor : IEventPredictor
{
    public Type EventType => typeof(TrashHeap);

    private static readonly RelicModel[] Relics = new RelicModel[5]
    {
        ModelDb.Relic<DarkstonePeriapt>(),
        ModelDb.Relic<DreamCatcher>(),
        ModelDb.Relic<HandDrill>(),
        ModelDb.Relic<MawBank>(),
        ModelDb.Relic<TheBoot>()
    };

    private static readonly CardModel[] Cards = new CardModel[10]
    {
        ModelDb.Card<Caltrops>(),
        ModelDb.Card<Clash>(),
        ModelDb.Card<Distraction>(),
        ModelDb.Card<DualWield>(),
        ModelDb.Card<Entrench>(),
        ModelDb.Card<HelloWorld>(),
        ModelDb.Card<Outmaneuver>(),
        ModelDb.Card<Rebound>(),
        ModelDb.Card<RipAndTear>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Stack>()
    };

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        // TrashHeap has two initial options; both branches roll from the same starting counter.
        var cardPeekRng = new Rng(mirrorRng.Seed, mirrorRng.Counter);
        int cardIndex = cardPeekRng.NextInt(0, Cards.Length);
        string cardName = LocText.Of(Cards[cardIndex]);

        var relicPeekRng = new Rng(mirrorRng.Seed, mirrorRng.Counter);
        int relicIndex = relicPeekRng.NextInt(0, Relics.Length);
        string relicName = LocText.Of(Relics[relicIndex]);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Grab", "抓取"),
                STS2AdvisorI18n.Pick($"Will gain {cardName}.", $"将获得 {cardName}。"),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Dive In", "跳进去"),
                STS2AdvisorI18n.Pick($"Will obtain relic: {relicName}.", $"将获得遗物：{relicName}。"),
                PredictionTag.Good)
        };
    }
}
