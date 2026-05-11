using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rewards;
using PartyObserver.Networking;

namespace PartyObserver.Services;

internal static class PartyObserverChoiceSnapshotBuilder
{
	private static readonly Regex BbCodePattern = new Regex("\\[[^\\]]+\\]", RegexOptions.Compiled);

	private static readonly Regex WhitespacePattern = new Regex("\\s+", RegexOptions.Compiled);

	private static readonly FieldInfo? RelicRewardRelicField = AccessTools.Field(typeof(RelicReward), "_relic");

	private static readonly FieldInfo? CardRewardCardsField = AccessTools.Field(typeof(CardReward), "_cards");

	private static readonly MethodInfo? RewardIconPathGetter = AccessTools.PropertyGetter(typeof(Reward), "IconPath");

	public static PartyObserverChoiceSnapshot BuildRewardsScreen(IEnumerable<Reward> rewards)
	{
		List<Reward> list = rewards.ToList();
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = new PartyObserverChoiceSnapshot
		{
			Kind = PartyObserverChoiceSnapshotKind.Rewards,
			ScreenLabel = PartyObserverChoiceSnapshotKind.Rewards.GetDisplayName(),
			Title = PartyObserverText.ReviewingRewards(),
			Description = PartyObserverText.FormatRewardsCount(list.Count)
		};
		foreach (Reward item in list)
		{
			partyObserverChoiceSnapshot.AddOption(BuildRewardOption(item));
		}
		return partyObserverChoiceSnapshot;
	}

	public static PartyObserverChoiceSnapshot BuildCardRewardSelection(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> extraOptions)
	{
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = new PartyObserverChoiceSnapshot
		{
			Kind = PartyObserverChoiceSnapshotKind.CardRewardSelection,
			ScreenLabel = PartyObserverChoiceSnapshotKind.CardRewardSelection.GetDisplayName(),
			Title = PartyObserverText.ChoosingCard(),
			Description = PartyObserverText.FormatCardOptionsCount(options.Count)
		};
		foreach (CardCreationResult option in options)
		{
			partyObserverChoiceSnapshot.AddOption(BuildCardOption(option));
		}
		foreach (CardRewardAlternative extraOption in extraOptions)
		{
			partyObserverChoiceSnapshot.AddOption(new PartyObserverChoiceOption
			{
				Title = Sanitize(PartyObserverGameText.ResolveLocString(extraOption.Title)),
				Subtitle = (string.IsNullOrWhiteSpace(extraOption.Hotkey) ? PartyObserverText.Action() : (PartyObserverText.Action() + " - " + extraOption.Hotkey)),
				Tag = "Action"
			});
		}
		return partyObserverChoiceSnapshot;
	}

	public static PartyObserverChoiceSnapshot BuildEventChoices(NEventLayout eventLayout)
	{
		List<NEventOptionButton> list = eventLayout.OptionButtons.ToList();
		NEventOptionButton val = list.FirstOrDefault();
		string text = ((val != null) ? PartyObserverGameText.ResolveLocString(val.Event.Title) : string.Empty);
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = new PartyObserverChoiceSnapshot
		{
			Kind = PartyObserverChoiceSnapshotKind.EventChoices,
			ScreenLabel = PartyObserverChoiceSnapshotKind.EventChoices.GetDisplayName(),
			Title = (string.IsNullOrWhiteSpace(text) ? PartyObserverText.Event() : Sanitize(text)),
			Description = PartyObserverText.FormatEventOptionsCount(list.Count)
		};
		foreach (NEventOptionButton item in list)
		{
			partyObserverChoiceSnapshot.AddOption(BuildEventOption(item));
		}
		return partyObserverChoiceSnapshot;
	}

	public static PartyObserverChoiceSnapshot BuildRelicSelection(IReadOnlyList<RelicModel> relics)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = new PartyObserverChoiceSnapshot
		{
			Kind = PartyObserverChoiceSnapshotKind.RelicSelection,
			ScreenLabel = PartyObserverChoiceSnapshotKind.RelicSelection.GetDisplayName(),
			Title = PartyObserverText.ChoosingRelic(),
			Description = PartyObserverText.FormatRelicOptionsCount(relics.Count)
		};
		foreach (RelicModel relic in relics)
		{
			partyObserverChoiceSnapshot.AddOption(new PartyObserverChoiceOption
			{
				Title = Sanitize(PartyObserverGameText.ResolveRelicTitle(relic)),
				Subtitle = PartyObserverText.GetRelicRarity(relic.Rarity) + " " + PartyObserverText.Relic(),
				Description = NormalizeRichText(PartyObserverGameText.ResolveRelicDescription(relic)),
				Tag = "Relic",
				ImagePath = PartyObserverGameText.ResolveRelicImagePath(relic)
			});
		}
		return partyObserverChoiceSnapshot;
	}

	public static PartyObserverChoiceSnapshot BuildMerchantInventory(MerchantInventory inventory)
	{
		List<PartyObserverChoiceOption> list = new List<PartyObserverChoiceOption>();
		list.AddRange(inventory.CharacterCardEntries.Where((MerchantCardEntry entry) => entry.CreationResult != null).Select(BuildMerchantCardOption));
		list.AddRange(inventory.ColorlessCardEntries.Where((MerchantCardEntry entry) => entry.CreationResult != null).Select(BuildMerchantCardOption));
		list.AddRange(inventory.RelicEntries.Where((MerchantRelicEntry entry) => entry.Model != null).Select(BuildMerchantRelicOption));
		list.AddRange(inventory.PotionEntries.Where((MerchantPotionEntry entry) => entry.Model != null).Select(BuildMerchantPotionOption));
		if (inventory.CardRemovalEntry != null)
		{
			list.Add(BuildMerchantCardRemovalOption(inventory.CardRemovalEntry));
		}
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = new PartyObserverChoiceSnapshot
		{
			Kind = PartyObserverChoiceSnapshotKind.MerchantInventory,
			ScreenLabel = PartyObserverChoiceSnapshotKind.MerchantInventory.GetDisplayName(),
			Title = PartyObserverText.BrowsingShop(),
			Description = PartyObserverText.FormatShopInventory(inventory.Player.Gold, list.Count)
		};
		foreach (PartyObserverChoiceOption item in list)
		{
			partyObserverChoiceSnapshot.AddOption(item);
		}
		return partyObserverChoiceSnapshot;
	}

	private static PartyObserverChoiceOption BuildCardOption(CardCreationResult result)
	{
		CardModel card = result.Card;
		return new PartyObserverChoiceOption
		{
			Title = Sanitize(PartyObserverGameText.ResolveCardTitle(card)),
			Subtitle = BuildCardSubtitle(card, result.HasBeenModified),
			Description = NormalizeRichText(PartyObserverGameText.ResolveCardDescription(card)),
			Tag = "Card",
			ImagePath = PartyObserverGameText.ResolveCardImagePath(card)
		};
	}

	private static PartyObserverChoiceOption BuildEventOption(NEventOptionButton button)
	{
		EventOption option = button.Option;
		string text = Sanitize(PartyObserverGameText.ResolveLocString(option.Title));
		string text2 = NormalizeRichText(PartyObserverGameText.ResolveLocString(option.Description));
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Sanitize(text2);
			text2 = string.Empty;
		}
		return new PartyObserverChoiceOption
		{
			Title = text,
			Subtitle = (option.IsLocked ? PartyObserverText.Locked() : (option.IsProceed ? PartyObserverText.Proceed() : PartyObserverText.EventChoice())),
			Description = text2,
			Tag = (option.IsProceed ? "Proceed" : "Event"),
			IsDisabled = option.IsLocked,
			IsProceed = option.IsProceed
		};
	}

	private static PartyObserverChoiceOption BuildRewardOption(Reward reward)
	{
		if (1 == 0)
		{
		}
		RelicReward val = (RelicReward)(object)((reward is RelicReward) ? reward : null);
		PartyObserverChoiceOption result;
		if (val == null)
		{
			PotionReward val2 = (PotionReward)(object)((reward is PotionReward) ? reward : null);
			if (val2 == null)
			{
				CardReward val3 = (CardReward)(object)((reward is CardReward) ? reward : null);
				result = ((val3 == null) ? new PartyObserverChoiceOption
				{
					Title = BuildRewardTitle(reward),
					Subtitle = BuildRewardSubtitle(reward),
					Description = BuildRewardDescription(reward),
					Tag = BuildRewardTag(reward),
					ImagePath = ResolveRewardIconPath(reward)
				} : BuildCardRewardOption(val3));
			}
			else
			{
				result = BuildPotionRewardOption(val2);
			}
		}
		else
		{
			result = BuildRelicRewardOption(val);
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private static PartyObserverChoiceOption BuildRelicRewardOption(RelicReward reward)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		object? obj = RelicRewardRelicField?.GetValue(reward);
		RelicModel val = (RelicModel)((obj is RelicModel) ? obj : null);
		return new PartyObserverChoiceOption
		{
			Title = ((val == null) ? PartyObserverText.RelicReward() : Sanitize(PartyObserverGameText.ResolveRelicTitle(val))),
			Subtitle = ((val == null) ? PartyObserverText.RelicReward() : (PartyObserverText.GetRelicRarity(val.Rarity) + " " + PartyObserverText.Relic())),
			Description = ((val == null) ? PartyObserverText.RewardAvailable() : NormalizeRichText(PartyObserverGameText.ResolveRelicDescription(val))),
			Tag = "Relic",
			ImagePath = ((val == null) ? ResolveRewardIconPath((Reward)(object)reward) : PartyObserverGameText.ResolveRelicImagePath(val))
		};
	}

	private static PartyObserverChoiceOption BuildPotionRewardOption(PotionReward reward)
	{
		PotionModel potion = reward.Potion;
		return new PartyObserverChoiceOption
		{
			Title = ((potion == null) ? PartyObserverText.PotionReward() : Sanitize(PartyObserverGameText.ResolvePotionTitle(potion))),
			Subtitle = PartyObserverText.PotionReward(),
			Description = ((potion == null) ? BuildRewardDescription((Reward)(object)reward) : NormalizeRichText(PartyObserverGameText.ResolvePotionDescription(potion))),
			Tag = "Potion",
			ImagePath = ((potion == null) ? ResolveRewardIconPath((Reward)(object)reward) : PartyObserverGameText.ResolvePotionImagePath(potion))
		};
	}

	private static PartyObserverChoiceOption BuildCardRewardOption(CardReward reward)
	{
		List<CardCreationResult> cardRewardCards = GetCardRewardCards(reward);
		List<string> list = (from result in cardRewardCards
			select Sanitize(PartyObserverGameText.ResolveCardTitle(result.Card)) into title
			where !string.IsNullOrWhiteSpace(title)
			select title).Take(3).ToList();
		PartyObserverChoiceOption obj = new PartyObserverChoiceOption
		{
			Title = PartyObserverText.CardReward(),
			Subtitle = PartyObserverText.FormatCardOptionsCount(cardRewardCards.Count),
			Description = ((list.Count == 0) ? BuildRewardDescription((Reward)(object)reward) : string.Join(" / ", list)),
			Tag = "Card"
		};
		CardCreationResult val = cardRewardCards.FirstOrDefault();
		string imagePath;
		if (val != null)
		{
			CardModel card = val.Card;
			if (card != null)
			{
				imagePath = PartyObserverGameText.ResolveCardImagePath(card);
				goto IL_00d1;
			}
		}
		imagePath = ResolveRewardIconPath((Reward)(object)reward);
		goto IL_00d1;
		IL_00d1:
		obj.ImagePath = imagePath;
		return obj;
	}

	private static List<CardCreationResult> GetCardRewardCards(CardReward reward)
	{
		return (CardRewardCardsField?.GetValue(reward) as List<CardCreationResult>) ?? new List<CardCreationResult>();
	}

	private static string BuildRewardTitle(Reward reward)
	{
		string text = BuildRewardTag(reward);
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"Gold" => PartyObserverText.GoldReward(), 
			"Potion" => PartyObserverText.PotionReward(), 
			"Card" => PartyObserverText.CardReward(), 
			"Relic" => PartyObserverText.RelicReward(), 
			"Action" => PartyObserverText.RewardAction(), 
			_ => PartyObserverText.Reward(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string BuildRewardSubtitle(Reward reward)
	{
		string text = BuildRewardTag(reward);
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"Gold" => PartyObserverText.Resource(), 
			"Potion" => PartyObserverText.Consumable(), 
			"Card" => PartyObserverText.DeckReward(), 
			"Relic" => PartyObserverText.PermanentItem(), 
			"Action" => PartyObserverText.RewardAction(), 
			_ => PartyObserverText.Reward(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string BuildRewardDescription(Reward reward)
	{
		try
		{
			return NormalizeRichText(PartyObserverGameText.ResolveLocString(reward.Description));
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string BuildRewardTag(Reward reward)
	{
		if (1 == 0)
		{
		}
		string result = ((reward is RelicReward) ? "Relic" : ((reward is CardReward) ? "Card" : ((reward is GoldReward) ? "Gold" : ((reward is PotionReward) ? "Potion" : ((reward is CardRemovalReward) ? "Action" : ((!(reward is SpecialCardReward)) ? "Reward" : "Card"))))));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string ResolveRewardIconPath(Reward reward)
	{
		try
		{
			return (RewardIconPathGetter?.Invoke(reward, null) as string) ?? string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string BuildCardSubtitle(CardModel card, bool isModified)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>
		{
			PartyObserverText.GetCardType(card.Type),
			PartyObserverText.GetCardRarity(card.Rarity),
			PartyObserverText.FormatCost(DescribeCardCost(card))
		};
		if (isModified)
		{
			list.Add(PartyObserverText.Modified());
		}
		return string.Join(" - ", list);
	}

	private static PartyObserverChoiceOption BuildMerchantCardOption(MerchantCardEntry entry)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		CardCreationResult val = entry.CreationResult ?? throw new InvalidOperationException("Merchant card entry does not have a card.");
		CardModel card = val.Card;
		List<string> list = new List<string>
		{
			PartyObserverText.GetCardType(card.Type),
			PartyObserverText.GetCardRarity(card.Rarity),
			PartyObserverText.FormatCost(DescribeCardCost(card)),
			PartyObserverText.FormatGoldAmount(((MerchantEntry)entry).Cost)
		};
		if (val.HasBeenModified)
		{
			list.Add(PartyObserverText.Modified());
		}
		if (entry.IsOnSale)
		{
			list.Add(PartyObserverText.OnSale());
		}
		return new PartyObserverChoiceOption
		{
			Title = Sanitize(PartyObserverGameText.ResolveCardTitle(card)),
			Subtitle = string.Join(" - ", list),
			Description = NormalizeRichText(PartyObserverGameText.ResolveCardDescription(card)),
			Tag = "Card",
			ImagePath = PartyObserverGameText.ResolveCardImagePath(card),
			IsDisabled = !((MerchantEntry)entry).EnoughGold
		};
	}

	private static PartyObserverChoiceOption BuildMerchantRelicOption(MerchantRelicEntry entry)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		RelicModel val = entry.Model ?? throw new InvalidOperationException("Merchant relic entry does not have a relic.");
		PartyObserverChoiceOption partyObserverChoiceOption = new PartyObserverChoiceOption();
		partyObserverChoiceOption.Title = Sanitize(PartyObserverGameText.ResolveRelicTitle(val));
		partyObserverChoiceOption.Subtitle = string.Join(" - ", PartyObserverText.GetRelicRarity(val.Rarity) + " " + PartyObserverText.Relic(), PartyObserverText.FormatGoldAmount(((MerchantEntry)entry).Cost));
		partyObserverChoiceOption.Description = NormalizeRichText(PartyObserverGameText.ResolveRelicDescription(val));
		partyObserverChoiceOption.Tag = "Relic";
		partyObserverChoiceOption.ImagePath = PartyObserverGameText.ResolveRelicImagePath(val);
		partyObserverChoiceOption.IsDisabled = !((MerchantEntry)entry).EnoughGold;
		return partyObserverChoiceOption;
	}

	private static PartyObserverChoiceOption BuildMerchantPotionOption(MerchantPotionEntry entry)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		PotionModel val = entry.Model ?? throw new InvalidOperationException("Merchant potion entry does not have a potion.");
		PartyObserverChoiceOption partyObserverChoiceOption = new PartyObserverChoiceOption();
		partyObserverChoiceOption.Title = Sanitize(PartyObserverGameText.ResolvePotionTitle(val));
		partyObserverChoiceOption.Subtitle = string.Join(" - ", PartyObserverText.GetPotionRarity(val.Rarity) + " " + PartyObserverText.Potion(), PartyObserverText.FormatGoldAmount(((MerchantEntry)entry).Cost));
		partyObserverChoiceOption.Description = NormalizeRichText(PartyObserverGameText.ResolvePotionDescription(val));
		partyObserverChoiceOption.Tag = "Potion";
		partyObserverChoiceOption.ImagePath = PartyObserverGameText.ResolvePotionImagePath(val);
		partyObserverChoiceOption.IsDisabled = !((MerchantEntry)entry).EnoughGold;
		return partyObserverChoiceOption;
	}

	private static PartyObserverChoiceOption BuildMerchantCardRemovalOption(MerchantCardRemovalEntry entry)
	{
		List<string> list = new List<string>
		{
			PartyObserverText.ShopService(),
			PartyObserverText.FormatGoldAmount(((MerchantEntry)entry).Cost)
		};
		if (entry.Used)
		{
			list.Add(PartyObserverText.Used());
		}
		return new PartyObserverChoiceOption
		{
			Title = PartyObserverText.CardRemoval(),
			Subtitle = string.Join(" - ", list),
			Description = (entry.Used ? PartyObserverText.CardRemovalUsedDescription() : PartyObserverText.CardRemovalDescription()),
			Tag = "Action",
			IsDisabled = (entry.Used || !((MerchantEntry)entry).EnoughGold)
		};
	}

	private static string DescribeCardCost(CardModel card)
	{
		return card.EnergyCost.CostsX ? "X" : card.EnergyCost.GetResolved().ToString();
	}

	private static string NormalizeRichText(string? rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return string.Empty;
		}
		return rawText.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
	}

	private static string Sanitize(string? rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return string.Empty;
		}
		string input = rawText.Replace('\r', ' ').Replace('\n', ' ');
		input = BbCodePattern.Replace(input, string.Empty);
		input = WhitespacePattern.Replace(input, " ");
		return input.Trim();
	}
}
