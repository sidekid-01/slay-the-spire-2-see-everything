using MegaCrit.Sts2.Core.Entities.Players;
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

public class SelfHelpBookPredictor : IEventPredictor
{
    public Type EventType => typeof(SelfHelpBook);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var player = eventModel.Owner;
        if (player == null)
            return new();

        bool hasAttack = HasEnchantableCards<Sharp>(player, CardType.Attack);
        bool hasSkill = HasEnchantableCards<Nimble>(player, CardType.Skill);
        bool hasPower = HasEnchantableCards<Swift>(player, CardType.Power);

        if (!hasAttack && !hasSkill && !hasPower)
        {
            return new()
            {
                new(
                    STS2AdvisorI18n.Pick("Available option", "可用选项"),
                    STS2AdvisorI18n.Pick("No valid cards; event will only offer skip.", "没有可用目标卡牌；事件只会提供跳过。"),
                    PredictionTag.Normal)
            };
        }

        return new()
        {
            BuildRow(
                STS2AdvisorI18n.Pick("Read the back", "阅读封底"),
                hasAttack,
                STS2AdvisorI18n.Pick("Choose 1 Attack to gain Sharp +2.", "选择 1 张攻击牌，获得 锋利 +2。")),
            BuildRow(
                STS2AdvisorI18n.Pick("Read passage", "阅读章节"),
                hasSkill,
                STS2AdvisorI18n.Pick("Choose 1 Skill to gain Nimble +2.", "选择 1 张技能牌，获得 敏捷 +2。")),
            BuildRow(
                STS2AdvisorI18n.Pick("Read entire book", "通读全书"),
                hasPower,
                STS2AdvisorI18n.Pick("Choose 1 Power to gain Swift +2.", "选择 1 张能力牌，获得 迅捷 +2。")),
        };
    }

    private static EventPrediction BuildRow(string label, bool available, string effect)
    {
        string value = available
            ? effect
            : STS2AdvisorI18n.Pick("Locked (no valid target card).", "已锁定（没有可用目标卡）。");
        return new EventPrediction(label, value, available ? PredictionTag.Good : PredictionTag.Normal);
    }

    private static bool HasEnchantableCards<T>(Player player, CardType typeRestriction) where T : EnchantmentModel
    {
        EnchantmentModel enchantment = ModelDb.Enchantment<T>();
        return PileType.Deck.GetPile(player).Cards.Any(card =>
            card != null &&
            card.Pile?.Type == PileType.Deck &&
            card.Type == typeRestriction &&
            enchantment.CanEnchant(card));
    }
}
