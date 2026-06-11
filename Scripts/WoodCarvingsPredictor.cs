using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class WoodCarvingsPredictor : IEventPredictor
{
    public Type EventType => typeof(WoodCarvings);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var deck = PileType.Deck.GetPile(owner).Cards;
        int basicTransformable = deck.Count(c => c != null && c.IsTransformable && c.Rarity == CardRarity.Basic);
        int slitherTargets = deck.Count(c => ModelDb.Enchantment<Slither>().CanEnchant(c));

        var rows = new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Bird", "飞鸟"),
                STS2AdvisorI18n.Pick(
                    "Choose 1 transformable Basic card; transform it into Peck.",
                    "选择 1 张可变形的基础牌；将其定向变形为 Peck。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Valid basic targets: {basicTransformable}.",
                        $"可用基础目标：{basicTransformable}。"),
                basicTransformable > 0 ? PredictionTag.Warning : PredictionTag.Normal),
            BuildSnakeRow(slitherTargets),
            new(
                STS2AdvisorI18n.Pick("Torus", "环体"),
                STS2AdvisorI18n.Pick(
                    "Choose 1 transformable Basic card; transform it into ToricToughness.",
                    "选择 1 张可变形的基础牌；将其定向变形为 ToricToughness。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Valid basic targets: {basicTransformable}.",
                        $"可用基础目标：{basicTransformable}。"),
                basicTransformable > 0 ? PredictionTag.Warning : PredictionTag.Normal)
        };

        return rows;
    }

    private static EventPrediction BuildSnakeRow(int slitherTargets)
    {
        if (slitherTargets <= 0)
        {
            return new EventPrediction(
                STS2AdvisorI18n.Pick("Snake", "蛇形"),
                STS2AdvisorI18n.Pick(
                    "Locked (no valid card for Slither).",
                    "已锁定（没有可附加 Slither 的卡牌）。"),
                PredictionTag.Normal);
        }

        return new EventPrediction(
            STS2AdvisorI18n.Pick("Snake", "蛇形"),
            STS2AdvisorI18n.Pick(
                "Choose 1 card to gain Slither +1.",
                "选择 1 张牌获得 Slither +1。")
                + " "
                + STS2AdvisorI18n.Pick(
                    $"Valid targets: {slitherTargets}.",
                    $"可用目标：{slitherTargets}。"),
            PredictionTag.Good);
    }
}
