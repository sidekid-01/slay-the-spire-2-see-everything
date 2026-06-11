using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class TezcataraPredictor : IEventPredictor
{
    public Type EventType => typeof(Tezcatara);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var pool1 = new List<RelicModel>
        {
            ModelDb.Relic<NutritiousSoup>(),
            ModelDb.Relic<VeryHotCocoa>(),
            ModelDb.Relic<YummyCookie>()
        };
        var pool2 = new List<RelicModel>
        {
            ModelDb.Relic<BiiigHug>(),
            ModelDb.Relic<Storybook>(),
            ModelDb.Relic<SealOfGold>(),
            ModelDb.Relic<ToastyMittens>()
        };
        var pool3 = new List<RelicModel>
        {
            ModelDb.Relic<GoldenCompass>(),
            ModelDb.Relic<PumpkinCandle>(),
            ModelDb.Relic<ToyBox>()
        };

        RelicModel pick1 = pool1[mirrorRng.NextInt(0, pool1.Count)];
        RelicModel pick2 = pool2[mirrorRng.NextInt(0, pool2.Count)];
        RelicModel pick3 = pool3[mirrorRng.NextInt(0, pool3.Count)];

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Pool 1 roll", "池1抽取"),
                LocText.Of(pick1),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pool 2 roll", "池2抽取"),
                LocText.Of(pick2),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pool 3 roll", "池3抽取"),
                LocText.Of(pick3),
                PredictionTag.Warning)
        };
    }
}
