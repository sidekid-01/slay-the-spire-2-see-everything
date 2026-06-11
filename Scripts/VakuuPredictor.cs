using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;

namespace STS2Advisor.Scripts;

public class VakuuPredictor : IEventPredictor
{
    public Type EventType => typeof(Vakuu);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var pool1 = new List<RelicModel>
        {
            ModelDb.Relic<BloodSoakedRose>(),
            ModelDb.Relic<WhisperingEarring>(),
            ModelDb.Relic<Fiddle>()
        };
        var pool2 = new List<RelicModel>
        {
            ModelDb.Relic<PreservedFog>(),
            ModelDb.Relic<SereTalon>(),
            ModelDb.Relic<DistinguishedCape>()
        };
        var pool3 = new List<RelicModel>
        {
            ModelDb.Relic<ChoicesParadox>(),
            ModelDb.Relic<MusicBox>(),
            ModelDb.Relic<LordsParasol>(),
            ModelDb.Relic<JeweledMask>()
        };

        UnstableShuffle(pool1, mirrorRng);
        UnstableShuffle(pool2, mirrorRng);
        UnstableShuffle(pool3, mirrorRng);

        RelicModel pick1 = pool1[0];
        RelicModel pick2 = pool2[0];
        RelicModel pick3 = pool3[0];

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Pool 1 roll", "池1抽取"),
                LocText.Of(pick1),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pool 2 roll", "池2抽取"),
                LocText.Of(pick2) + (pick2 is DistinguishedCape
                    ? STS2AdvisorI18n.Pick(" (Costs 9 Max HP)", "（代价：-9 最大生命）")
                    : string.Empty),
                pick2 is DistinguishedCape ? PredictionTag.Bad : PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pool 3 roll", "池3抽取"),
                LocText.Of(pick3),
                PredictionTag.Warning)
        };

        return rows;
    }

    private static void UnstableShuffle(List<RelicModel> list, Rng rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int j = rng.NextInt(n + 1);
            (list[j], list[n]) = (list[n], list[j]);
        }
    }
}
