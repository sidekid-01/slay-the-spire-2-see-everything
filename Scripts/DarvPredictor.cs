using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class DarvPredictor : IEventPredictor
{
    public Type EventType => typeof(Darv);

    private sealed record RelicSet(Func<MegaCrit.Sts2.Core.Entities.Players.Player, bool> Filter, RelicModel[] Relics);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var sets = BuildValidRelicSets();
        var source = sets
            .Where(s => s.Filter(owner))
            .Select(s => s.Relics[mirrorRng.NextInt(0, s.Relics.Length)])
            .ToList();

        UnstableShuffle(source, mirrorRng);

        var picks = new List<RelicModel>();
        bool includeDustyTome = mirrorRng.NextBool();
        if (includeDustyTome)
        {
            picks.AddRange(source.Take(2));
            var dusty = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
            dusty.SetupForPlayer(owner);
            picks.Add(dusty);
        }
        else
        {
            picks.AddRange(source.Take(3));
        }

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Darv roll", "Darv 抽选"),
                includeDustyTome
                    ? STS2AdvisorI18n.Pick("Dusty Tome branch triggered.", "触发 Dusty Tome 分支。")
                    : STS2AdvisorI18n.Pick("Normal 3-option branch.", "普通三选一分支。"),
                PredictionTag.Normal)
        };

        for (int i = 0; i < picks.Count; i++)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Option {i + 1}", $"选项 {i + 1}"),
                LocText.Of(picks[i]),
                PredictionTag.Warning));
        }

        return rows;
    }

    private static List<RelicSet> BuildValidRelicSets()
    {
        return new List<RelicSet>
        {
            new(_ => true, new RelicModel[] { ModelDb.Relic<Astrolabe>() }),
            new(_ => true, new RelicModel[] { ModelDb.Relic<BlackStar>() }),
            new(_ => true, new RelicModel[] { ModelDb.Relic<CallingBell>() }),
            new(_ => true, new RelicModel[] { ModelDb.Relic<EmptyCage>() }),
            new(owner => !owner.RunState.Modifiers.Any(m => m.ClearsPlayerDeck), new RelicModel[] { ModelDb.Relic<PandorasBox>() }),
            new(_ => true, new RelicModel[] { ModelDb.Relic<RunicPyramid>() }),
            new(_ => true, new RelicModel[] { ModelDb.Relic<SneckoEye>() }),
            new(owner => owner.RunState.CurrentActIndex == 1, new RelicModel[] { ModelDb.Relic<Ectoplasm>(), ModelDb.Relic<Sozu>() }),
            new(owner => owner.RunState.CurrentActIndex == 2, new RelicModel[] { ModelDb.Relic<PhilosophersStone>(), ModelDb.Relic<VelvetChoker>() }),
        };
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
