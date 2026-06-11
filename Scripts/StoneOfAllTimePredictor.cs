using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

public class StoneOfAllTimePredictor : IEventPredictor
{
    public Type EventType => typeof(StoneOfAllTime);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int drinkMaxHp = ReadIntVar(eventModel, "DrinkMaxHpGain", 10);
        int pushHpLoss = ReadIntVar(eventModel, "PushHpLoss", 6);
        int pushVigorous = ReadIntVar(eventModel, "PushVigorousAmount", 8);

        var rows = new List<EventPrediction>();

        var potions = owner.Potions.ToList();
        if (potions.Count > 0)
        {
            int potionIndex = mirrorRng.NextInt(0, potions.Count);
            string potionName = LocText.Of(potions[potionIndex]);
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Lift", "抬起"),
                STS2AdvisorI18n.Pick(
                    $"Discard random potion: {potionName}; gain +{drinkMaxHp} Max HP.",
                    $"丢弃随机药水：{potionName}；获得 +{drinkMaxHp} 最大生命。"),
                PredictionTag.Good));
        }
        else
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Lift", "抬起"),
                STS2AdvisorI18n.Pick("Locked (no potion).", "已锁定（没有药水）。"),
                PredictionTag.Normal));
        }

        int enchantableCount = owner.Deck.Cards.Count(c => ModelDb.Enchantment<Vigorous>().CanEnchant(c));
        if (enchantableCount < 1)
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Push", "推动"),
                STS2AdvisorI18n.Pick("Locked (no valid card for Vigorous).", "已锁定（没有可附加刚健的卡牌）。"),
                PredictionTag.Normal));
        }
        else
        {
            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick("Push", "推动"),
                STS2AdvisorI18n.Pick(
                    $"Take {pushHpLoss} damage; choose 1 deck card to gain Vigorous +{pushVigorous}.",
                    $"受到 {pushHpLoss} 点伤害；选择 1 张牌组卡牌获得 刚健 +{pushVigorous}。"),
                PredictionTag.Warning));
        }

        return rows;
    }

    private static int ReadIntVar(EventModel eventModel, string key, int fallback)
    {
        if (!eventModel.DynamicVars.ContainsKey(key))
            return fallback;

        if (eventModel.DynamicVars[key] is DynamicVar dv)
            return dv.IntValue;

        return fallback;
    }
}
