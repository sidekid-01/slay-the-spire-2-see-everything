using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class OrobasPredictor : IEventPredictor
{
    public Type EventType => typeof(Orobas);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var otherChars = owner.UnlockState.Characters.Where(c => c.Id != owner.Character.Id).ToList();
        var chosenCharacter = otherChars.Count > 0
            ? otherChars[mirrorRng.NextInt(0, otherChars.Count)]
            : owner.Character;

        var pool1 = new List<RelicModel>
        {
            ModelDb.Relic<ElectricShrymp>(),
            ModelDb.Relic<GlassEye>(),
            ModelDb.Relic<SandCastle>()
        };

        if (mirrorRng.NextFloat() < 0.3333333f)
        {
            pool1.Add(ModelDb.Relic<PrismaticGem>());
        }
        else
        {
            var seaGlass = (SeaGlass)ModelDb.Relic<SeaGlass>().ToMutable();
            seaGlass.CharacterId = chosenCharacter.Id;
            pool1.Add(seaGlass);
        }

        var pool2 = new List<RelicModel>
        {
            ModelDb.Relic<AlchemicalCoffer>(),
            ModelDb.Relic<Driftwood>(),
            ModelDb.Relic<RadiantPearl>()
        };

        var pool3 = BuildPool3(owner);
        if (pool3.Count == 0)
        {
            return new List<EventPrediction>
            {
                new(
                    STS2AdvisorI18n.Pick("Orobas option 1", "Orobas 选项 1"),
                    LocText.Of(pool1[mirrorRng.NextInt(0, pool1.Count)]),
                    PredictionTag.Warning),
                new(
                    STS2AdvisorI18n.Pick("Orobas option 2", "Orobas 选项 2"),
                    LocText.Of(pool2[mirrorRng.NextInt(0, pool2.Count)]),
                    PredictionTag.Warning),
                new(
                    STS2AdvisorI18n.Pick("Orobas option 3", "Orobas 选项 3"),
                    STS2AdvisorI18n.Pick("Locked (pool has no valid relic).", "锁定（该池没有可用遗物）。"),
                    PredictionTag.Normal)
            };
        }

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Orobas option 1", "Orobas 选项 1"),
                LocText.Of(pool1[mirrorRng.NextInt(0, pool1.Count)]),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Orobas option 2", "Orobas 选项 2"),
                LocText.Of(pool2[mirrorRng.NextInt(0, pool2.Count)]),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Orobas option 3", "Orobas 选项 3"),
                LocText.Of(pool3[mirrorRng.NextInt(0, pool3.Count)]),
                PredictionTag.Warning)
        };
    }

    private static List<RelicModel> BuildPool3(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var list = new List<RelicModel>();

        var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        if (touch.SetupForPlayer(owner))
            list.Add(touch);

        var tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
        if (tooth.SetupForPlayer(owner))
            list.Add(tooth);

        return list;
    }
}
