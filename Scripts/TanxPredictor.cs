using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class TanxPredictor : IEventPredictor
{
    public Type EventType => typeof(Tanx);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var pool = new List<RelicModel>
        {
            ModelDb.Relic<Claws>(),
            ModelDb.Relic<Crossbow>(),
            ModelDb.Relic<IronClub>(),
            ModelDb.Relic<MeatCleaver>(),
            ModelDb.Relic<Sai>(),
            ModelDb.Relic<SpikedGauntlets>(),
            ModelDb.Relic<TanxsWhistle>(),
            ModelDb.Relic<ThrowingAxe>(),
            ModelDb.Relic<WarHammer>()
        };

        int instinctTargets = owner.Deck.Cards.Count(c => ModelDb.Enchantment<Instinct>().CanEnchant(c));
        bool apexAvailable = instinctTargets >= 3;
        if (apexAvailable)
            pool.Add(ModelDb.Relic<TriBoomerang>());

        UnstableShuffle(pool, mirrorRng);
        var picks = pool.Take(3).ToList();

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Apex unlock", "Apex 解锁"),
                apexAvailable
                    ? STS2AdvisorI18n.Pick("Unlocked (>=3 Instinct-enchantable cards).", "已解锁（可附加 Instinct 的卡牌 >= 3）。")
                    : STS2AdvisorI18n.Pick("Locked (<3 Instinct-enchantable cards).", "未解锁（可附加 Instinct 的卡牌 < 3）。"),
                apexAvailable ? PredictionTag.Good : PredictionTag.Normal)
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
