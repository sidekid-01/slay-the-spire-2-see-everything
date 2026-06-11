using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class PaelPredictor : IEventPredictor
{
    public Type EventType => typeof(Pael);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        RelicModel option1 = RollPool1(mirrorRng);
        RelicModel option2 = RollPool2(owner, mirrorRng);
        RelicModel option3 = RollPool3(owner, mirrorRng);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Pael option 1", "Pael 选项 1"),
                LocText.Of(option1),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pael option 2", "Pael 选项 2"),
                LocText.Of(option2),
                PredictionTag.Warning),
            new(
                STS2AdvisorI18n.Pick("Pael option 3", "Pael 选项 3"),
                LocText.Of(option3),
                PredictionTag.Warning)
        };
    }

    private static RelicModel RollPool1(Rng rng)
    {
        var pool = new List<RelicModel>
        {
            ModelDb.Relic<PaelsFlesh>(),
            ModelDb.Relic<PaelsHorn>(),
            ModelDb.Relic<PaelsTears>()
        };
        int idx = rng.NextInt(0, pool.Count);
        return pool[idx];
    }

    private static RelicModel RollPool2(MegaCrit.Sts2.Core.Entities.Players.Player owner, Rng rng)
    {
        var cards = owner.Deck.Cards;
        var basePool = new List<RelicModel>
        {
            ModelDb.Relic<PaelsWing>()
        };

        int goopyEligible = cards.Count(c => ModelDb.Enchantment<Goopy>().CanEnchant(c));
        if (goopyEligible >= 3)
            basePool.Add(ModelDb.Relic<PaelsClaw>());

        int removable = cards.Count(c => c.IsRemovable);
        if (removable >= 5)
            basePool.Add(ModelDb.Relic<PaelsTooth>());

        // Mirror event logic: duplicate current pool for weighting, then append Growth once.
        var weighted = basePool.ToList();
        weighted.AddRange(basePool);
        weighted.Add(ModelDb.Relic<PaelsGrowth>());

        int idx = rng.NextInt(0, weighted.Count);
        return weighted[idx];
    }

    private static RelicModel RollPool3(MegaCrit.Sts2.Core.Entities.Players.Player owner, Rng rng)
    {
        var pool = new List<RelicModel>
        {
            ModelDb.Relic<PaelsEye>(),
            ModelDb.Relic<PaelsBlood>()
        };

        if (!owner.HasEventPet())
            pool.Add(ModelDb.Relic<PaelsLegion>());

        int idx = rng.NextInt(0, pool.Count);
        return pool[idx];
    }
}
