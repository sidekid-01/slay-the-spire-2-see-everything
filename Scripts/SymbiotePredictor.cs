using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class SymbiotePredictor : IEventPredictor
{
    public Type EventType => typeof(Symbiote);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var deck = PileType.Deck.GetPile(owner).Cards;
        int enchantable = deck.Count(c => ModelDb.Enchantment<Corrupted>().CanEnchant(c));

        var rows = new List<EventPrediction>();
        if (enchantable > 0)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Approach", "接近"),
                STS2AdvisorI18n.Pick(
                    "Choose 1 card to gain Corrupted +1.",
                    "选择 1 张牌获得 Corrupted +1。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Valid targets: {enchantable}.",
                        $"可用目标：{enchantable}。"),
                PredictionTag.Good));
        }
        else
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Approach", "接近"),
                STS2AdvisorI18n.Pick(
                    "Locked (no valid card for Corrupted).",
                    "已锁定（没有可附加 Corrupted 的卡牌）。"),
                PredictionTag.Normal));
        }

        var transformable = deck.Where(c => c.IsTransformable).ToList();
        string transformInfo;
        if (transformable.Count == 0)
        {
            transformInfo = EventPredictionText.NoTransformableCards();
        }
        else
        {
            // Symbiote transforms exactly base.DynamicVars.Cards (=1) selected card.
            var byPool = transformable.GroupBy(GetPoolKey).ToList();
            if (byPool.Count == 1)
            {
                var pool = TransformPredictor.GetFilteredPool(byPool[0].First(), isInCombat: false);
                if (pool.Length == 0)
                {
                    transformInfo = EventPredictionText.NoTransformTargets();
                }
                else
                {
                    int idx = new Rng(mirrorRng.Seed, mirrorRng.Counter).NextInt(0, pool.Length);
                    transformInfo = STS2AdvisorI18n.Pick("Transform result: ", "变形结果：") + LocText.Of(pool[idx]);
                }
            }
            else
            {
                transformInfo = STS2AdvisorI18n.Pick(
                    "Transform 1 selected card (result depends on selected card pool).",
                    "变形 1 张自选牌（结果取决于所选卡牌的卡池）。");
            }
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Kill With Fire", "火焰净化"),
            transformInfo,
            PredictionTag.Warning));

        return rows;
    }

    private static string GetPoolKey(CardModel c)
    {
        bool isSpecial = c.Type == CardType.Quest
            || c.Rarity == CardRarity.Event
            || c.Rarity == CardRarity.Ancient
            || c.Rarity == CardRarity.Token;
        return isSpecial ? "colorless" : (c.Pool?.Id.Entry ?? "colorless");
    }
}
