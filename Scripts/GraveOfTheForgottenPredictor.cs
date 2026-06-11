using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class GraveOfTheForgottenPredictor : IEventPredictor
{
    public Type EventType => typeof(GraveOfTheForgotten);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        var soulsPower = ModelDb.Enchantment<SoulsPower>();
        int validSoulTargets = PileType.Deck.GetPile(owner).Cards.Count(c => soulsPower.CanEnchant(c));

        var rows = new List<EventPrediction>();
        if (validSoulTargets > 0)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Confront", "对抗"),
                STS2AdvisorI18n.Pick(
                    "Add Decay curse to deck, then choose 1 card to gain SoulsPower +1.",
                    "向牌组加入 Decay 诅咒，然后选择 1 张牌获得 灵魂之力 +1。")
                    + " "
                    + STS2AdvisorI18n.Pick(
                        $"Valid targets: {validSoulTargets}.",
                        $"可附加目标：{validSoulTargets}。"),
                PredictionTag.Warning));
        }
        else
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Confront", "对抗"),
                STS2AdvisorI18n.Pick(
                    "Locked (no card can receive SoulsPower).",
                    "已锁定（没有可附加灵魂之力的卡牌）。"),
                PredictionTag.Normal));
        }

        rows.Add(new EventPrediction(
            STS2AdvisorI18n.Pick("Accept", "接受"),
            STS2AdvisorI18n.Pick(
                "Obtain ForgottenSoul relic.",
                "获得 ForgottenSoul 遗物。"),
            PredictionTag.Good));

        return rows;
    }
}
