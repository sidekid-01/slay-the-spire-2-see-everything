using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class SapphireSeedPredictor : IEventPredictor
{
    public Type EventType => typeof(SapphireSeed);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int heal = eventModel.DynamicVars.Heal.IntValue;
        int upgradable = owner.Deck.Cards.Count(c => c.IsUpgradable);
        var sown = ModelDb.Enchantment<Sown>();
        int sownTargets = owner.Deck.Cards.Count(c => sown.CanEnchant(c));

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Eat", "食用"),
                STS2AdvisorI18n.Pick(
                    $"Heal {heal}, then choose 1 card to upgrade.",
                    $"回复 {heal} 生命，然后选择 1 张牌升级。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Upgradable cards: {upgradable}.",
                        $"可升级卡牌：{upgradable}。"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Plant", "种植"),
                STS2AdvisorI18n.Pick(
                    "Choose 1 card to gain Sown +1.",
                    "选择 1 张牌获得 播种 +1。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Valid targets: {sownTargets}.",
                        $"可附加目标：{sownTargets}。"),
                sownTargets > 0 ? PredictionTag.Warning : PredictionTag.Normal)
        };
    }
}
