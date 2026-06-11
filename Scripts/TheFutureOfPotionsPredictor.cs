using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace STS2Advisor.Scripts;

public class TheFutureOfPotionsPredictor : IEventPredictor
{
    public Type EventType => typeof(TheFutureOfPotions);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        List<PotionModel> potions = owner.Potions.ToList();
        int count = Mathf.Min(3, potions.Count);
        if (count == 0)
        {
            return new List<EventPrediction>
            {
                new(
                    STS2AdvisorI18n.Pick("Options", "选项"),
                    STS2AdvisorI18n.Pick("No potion available to trade.", "没有可用于交易的药水。"),
                    PredictionTag.Normal)
            };
        }

        var rows = new List<EventPrediction>();
        for (int i = 0; i < count; i++)
        {
            PotionModel potion = potions[i];
            string potionName = LocText.Of(potion);
            if (!TryResolveTradeRule(eventModel, potion, out CardRarity targetRarity, out CardType targetType))
            {
                rows.Add(new EventPrediction(
                    STS2AdvisorI18n.Pick($"Trade option {i + 1}", $"交易选项 {i + 1}"),
                    STS2AdvisorI18n.Pick(
                        $"Trade potion: {potionName}",
                        $"交易药水：{potionName}"),
                    PredictionTag.Warning));
                continue;
            }

            var rewardsRng = owner.PlayerRng.Rewards;
            var peekRng = new Rng(rewardsRng.Seed, rewardsRng.Counter);
            var predictedCards = SimulateTradeReward(owner, targetRarity, targetType, peekRng);
            string cardText = predictedCards.Count == 0
                ? STS2AdvisorI18n.Pick("No valid card candidates.", "没有可用卡牌候选。")
                : string.Join(" / ", predictedCards);

            rows.Add(new EventPrediction(
                STS2AdvisorI18n.Pick($"Trade option {i + 1}", $"交易选项 {i + 1}"),
                STS2AdvisorI18n.Pick(
                    $"Trade {potionName} -> {targetRarity} {targetType} cards (upgraded): {cardText}",
                    $"交易 {potionName} -> {targetRarity} {targetType} 卡（升级后）：{cardText}"),
                PredictionTag.Good));
        }

        return rows;
    }

    private static bool TryResolveTradeRule(
        EventModel eventModel,
        PotionModel potion,
        out CardRarity targetRarity,
        out CardType targetType)
    {
        targetRarity = CardRarity.None;
        targetType = CardType.None;
        Type t = eventModel.GetType();

        MethodInfo? rarityMethod = t.GetMethod("GetCardRarity", BindingFlags.Instance | BindingFlags.NonPublic);
        if (rarityMethod == null)
            return false;

        object? rarityObj = rarityMethod.Invoke(eventModel, new object[] { potion });
        if (rarityObj is not CardRarity rarity)
            return false;
        targetRarity = rarity;

        object? mapObj = t.GetProperty("PotionToCardType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(eventModel)
            ?? t.GetField("PotionToCardType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(eventModel);

        if (mapObj is IDictionary map && map.Contains(potion))
        {
            object? typeObj = map[potion];
            if (typeObj is CardType cardType)
            {
                targetType = cardType;
                return true;
            }
        }

        return false;
    }

    private static List<string> SimulateTradeReward(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        CardRarity targetRarity,
        CardType targetType,
        Rng peekRng)
    {
        var pool = owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == targetRarity && c.Type == targetType && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();

        var blacklist = new HashSet<string>();
        var results = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var candidates = pool.Where(c => !blacklist.Contains(c.Id.Entry)).ToList();
            if (candidates.Count == 0)
                break;

            int idx = peekRng.NextInt(0, candidates.Count);
            var card = candidates[idx];
            blacklist.Add(card.Id.Entry);
            peekRng.NextFloat();
            results.Add(LocText.Of(card) + STS2AdvisorI18n.Pick("+", "+"));
        }

        return results;
    }
}
