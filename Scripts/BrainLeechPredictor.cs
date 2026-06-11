using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

/// <summary>
/// BrainLeech: ShareKnowledge = CreateForReward(character pool, FromCardChoiceCount) pick 1 to deck;
/// Rip = RipHpLoss damage then RewardCount × CardReward(3, colorless pool, non-combat default odds).
/// Card peek mirrors <see cref="NeowPredictor"/> MassiveScroll (Rewards RNG: rarity floats + pick + upgrade float per card).
/// </summary>
public class BrainLeechPredictor : IEventPredictor
{
    public Type EventType => typeof(BrainLeech);

    public List<EventPrediction> Predict(EventModel eventModel, Rng mirrorRng)
    {
        var owner = eventModel.Owner;
        if (owner == null)
            return new();

        int ripHp = (int)eventModel.DynamicVars["RipHpLoss"].BaseValue;
        int rewardCount = eventModel.DynamicVars["RewardCount"].IntValue;
        int fromCount = eventModel.DynamicVars["FromCardChoiceCount"].IntValue;

        var rewards = owner.PlayerRng.Rewards;
        var peekShare = new Rng(rewards.Seed, rewards.Counter);
        var peekRip = new Rng(rewards.Seed, rewards.Counter);

        var charPool = BuildRewardPool(owner, owner.Character.CardPool);
        var colorlessPool = BuildRewardPool(owner, ModelDb.CardPool<ColorlessCardPool>());

        string shareCards = FormatSimulatedCards(charPool, fromCount, peekShare);
        string ripCards = FormatRipRewards(colorlessPool, rewardCount, peekRip);

        return new List<EventPrediction>
        {
            new(
                STS2AdvisorI18n.Pick("Share knowledge", "分享知识"),
                STS2AdvisorI18n.Pick(
                    $"Generate {fromCount} class cards (non-combat default odds), pick 1 for deck: {shareCards}",
                    $"按非战斗默认概率生成 {fromCount} 张职业牌，选 1 张加入牌组：{shareCards}"),
                PredictionTag.Good),
            new(
                STS2AdvisorI18n.Pick("Rip", "撕开"),
                STS2AdvisorI18n.Pick(
                    $"Take {ripHp} unblockable damage, then {rewardCount}× card reward (3 colorless each): {ripCards}",
                    $"受到 {ripHp} 点不可阻挡伤害，然后 {rewardCount} 次卡牌奖励（每次 3 张无色候选）：{ripCards}"),
                PredictionTag.Warning)
        };
    }

    private static string FormatRipRewards(List<CardModel> colorlessPool, int rewardCount, Rng peekRng)
    {
        if (rewardCount <= 0)
            return STS2AdvisorI18n.Pick("(No card rewards.)", "（无卡牌奖励。）");

        var parts = new List<string>();
        for (int r = 0; r < rewardCount; r++)
        {
            string batch = FormatSimulatedCards(colorlessPool, 3, peekRng);
            parts.Add(batch);
        }

        return string.Join(STS2AdvisorI18n.Pick(" | ", "｜"), parts);
    }

    private static string FormatSimulatedCards(List<CardModel> pool, int count, Rng peekRng)
    {
        if (pool.Count == 0)
            return STS2AdvisorI18n.Pick("No cards in pool.", "卡池无可用牌。");

        var names = SimulateRewardCards(pool, count, peekRng);
        return names.Count == 0
            ? STS2AdvisorI18n.Pick("Could not roll valid cards.", "未能掷出有效卡牌。")
            : string.Join(" / ", names);
    }

    private static List<CardModel> BuildRewardPool(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        CardPoolModel poolModel)
    {
        IEnumerable<CardModel> unlocked = poolModel.GetUnlockedCards(
            owner.UnlockState,
            owner.RunState.CardMultiplayerConstraint);

        if (owner.RunState.Players.Count > 1)
            unlocked = unlocked.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
        else
            unlocked = unlocked.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);

        return unlocked
            .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();
    }

    /// <summary>Same RNG stepping pattern as <see cref="NeowPredictor"/> PredictMassiveScroll.</summary>
    private static List<string> SimulateRewardCards(List<CardModel> fullPool, int count, Rng peekRng)
    {
        var blacklistIds = new HashSet<string>();
        var results = new List<string>();

        for (int i = 0; i < count; i++)
        {
            float rarityRoll = peekRng.NextFloat();
            CardRarity rarity;
            if (rarityRoll < CardRarityOdds.RegularRareOdds)
                rarity = CardRarity.Rare;
            else if (rarityRoll < 0.37f)
                rarity = CardRarity.Uncommon;
            else
                rarity = CardRarity.Common;

            var candidates = FilterByRarity(fullPool, rarity, blacklistIds);
            if (candidates.Count == 0) candidates = FilterByRarity(fullPool, CardRarity.Uncommon, blacklistIds);
            if (candidates.Count == 0) candidates = FilterByRarity(fullPool, CardRarity.Rare, blacklistIds);
            if (candidates.Count == 0) candidates = fullPool.Where(c => !blacklistIds.Contains(c.Id.Entry)).ToList();

            if (candidates.Count == 0)
            {
                peekRng.NextInt(1);
                peekRng.NextFloat();
                results.Add("?");
                continue;
            }

            int idx = peekRng.NextInt(0, candidates.Count);
            var card = candidates[idx];
            blacklistIds.Add(card.Id.Entry);
            peekRng.NextFloat();
            results.Add(LocText.Of(card));
        }

        return results;
    }

    private static List<CardModel> FilterByRarity(
        List<CardModel> pool,
        CardRarity rarity,
        HashSet<string> blacklistIds) =>
        pool.Where(c => c.Rarity == rarity && !blacklistIds.Contains(c.Id.Entry)).ToList();
}
