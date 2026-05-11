using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using PartyObserver.Networking;

namespace PartyObserver.Services;

internal static class PartyObserverText
{
	public const string ActionTagKey = "Action";

	public const string CardTagKey = "Card";

	public const string RelicTagKey = "Relic";

	public const string GoldTagKey = "Gold";

	public const string PotionTagKey = "Potion";

	public const string ProceedTagKey = "Proceed";

	public const string EventTagKey = "Event";

	public const string RewardTagKey = "Reward";

	public static string CurrentLanguageToken()
	{
		LocManager instance = LocManager.Instance;
		return NormalizeLanguage((instance != null) ? instance.Language : null);
	}

	public static string HoverHint()
	{
		return Pick("Click this panel for more details.", "点击这个窗口查看更详细的信息。", "點擊這個視窗查看更詳細的資訊。");
	}

	public static string Close()
	{
		return Pick("Close", "关闭", "關閉");
	}

	public static string FormatCurrentScreen(string screen)
	{
		return Pick("Current screen: " + screen, "当前界面：" + screen, "當前介面：" + screen);
	}

	public static string NoSnapshot()
	{
		return Pick("This teammate does not have a synced snapshot for the current screen yet.", "这名队友当前界面还没有同步快照。", "這名隊友目前畫面還沒有同步快照。");
	}

	public static string NoExtraDetails()
	{
		return Pick("No extra details are available for this snapshot.", "这个快照暂时没有更多详细信息。", "這個快照暫時沒有更多詳細資訊。");
	}

	public static string NoSyncedOptions()
	{
		return Pick("No synced options are available right now.", "当前没有可用的同步选项。", "目前沒有可用的同步選項。");
	}

	public static string SnapshotHasNoVisibleOptions()
	{
		return Pick("This snapshot has no visible options.", "这个快照里没有可见选项。", "這個快照裡沒有可見選項。");
	}

	public static string NoSyncedChoiceDetails()
	{
		return Pick("No synced choice details yet.", "暂时还没有同步到详细选项信息。", "暫時還沒有同步到詳細選項資訊。");
	}

	public static string FormatSyncedOptionsAvailable(int count)
	{
		return Pick($"Synced options: {count}", $"已同步选项：{count}", $"已同步選項：{count}");
	}

	public static string NoVisibleOptionsInSnapshot()
	{
		return Pick("No visible options in the current snapshot.", "当前快照中没有可见选项。", "目前快照中沒有可見選項。");
	}

	public static string Unknown()
	{
		return Pick("Unknown", "未知", "未知");
	}

	public static string ReviewingRewards()
	{
		return Pick("Reviewing rewards", "正在查看奖励", "正在查看獎勵");
	}

	public static string FormatRewardsCount(int count)
	{
		return Pick($"Rewards: {count}", $"奖励数量：{count}", $"獎勵數量：{count}");
	}

	public static string ChoosingRelic()
	{
		return Pick("Choosing a relic", "正在选择遗物", "正在選擇遺物");
	}

	public static string FormatRelicOptionsCount(int count)
	{
		return Pick($"Relics: {count}", $"遗物数量: {count}", $"遺物數量: {count}");
	}

	public static string RelicChoice()
	{
		return Pick("Relic choice", "遗物选择", "遺物選擇");
	}

	public static string ChoosingCard()
	{
		return Pick("Choosing a card", "正在选择卡牌", "正在選擇卡牌");
	}

	public static string Shop()
	{
		return Pick("Shop", "商店", "商店");
	}

	public static string BrowsingShop()
	{
		return Pick("Browsing the shop", "正在查看商店", "正在查看商店");
	}

	public static string FormatShopInventory(int gold, int count)
	{
		return Pick($"Offers: {count} · Gold: {gold}", $"商品：{count} · 金币：{gold}", $"商品：{count} · 金幣：{gold}");
	}

	public static string FormatCardOptionsCount(int count)
	{
		return Pick($"Card options: {count}", $"卡牌选项：{count}", $"卡牌選項：{count}");
	}

	public static string FormatEventOptionsCount(int count)
	{
		return Pick($"Options: {count}", $"选项数量：{count}", $"選項數量：{count}");
	}

	public static string Action()
	{
		return Pick("Action", "操作", "操作");
	}

	public static string Card()
	{
		return Pick("Card", "卡牌", "卡牌");
	}

	public static string Relic()
	{
		return Pick("Relic", "遗物", "遺物");
	}

	public static string Gold()
	{
		return Pick("Gold", "金币", "金幣");
	}

	public static string Potion()
	{
		return Pick("Potion", "药水", "藥水");
	}

	public static string Proceed()
	{
		return Pick("Proceed", "继续", "繼續");
	}

	public static string Locked()
	{
		return Pick("Locked", "已锁定", "已鎖定");
	}

	public static string EventChoice()
	{
		return Pick("Event choice", "事件选项", "事件選項");
	}

	public static string Event()
	{
		return Pick("Event", "事件", "事件");
	}

	public static string RelicReward()
	{
		return Pick("Relic reward", "遗物奖励", "遺物獎勵");
	}

	public static string CardReward()
	{
		return Pick("Card reward", "卡牌奖励", "卡牌獎勵");
	}

	public static string GoldReward()
	{
		return Pick("Gold reward", "金币奖励", "金幣獎勵");
	}

	public static string PotionReward()
	{
		return Pick("Potion reward", "药水奖励", "藥水獎勵");
	}

	public static string RewardAction()
	{
		return Pick("Reward action", "奖励操作", "獎勵操作");
	}

	public static string Reward()
	{
		return Pick("Reward", "奖励", "獎勵");
	}

	public static string Resource()
	{
		return Pick("Resource", "资源", "資源");
	}

	public static string Consumable()
	{
		return Pick("Consumable", "消耗品", "消耗品");
	}

	public static string DeckReward()
	{
		return Pick("Deck reward", "牌组奖励", "牌組獎勵");
	}

	public static string PermanentItem()
	{
		return Pick("Permanent item", "永久物品", "永久物品");
	}

	public static string FormatCost(string value)
	{
		return Pick("Cost " + value, "费用 " + value, "費用 " + value);
	}

	public static string Modified()
	{
		return Pick("Modified", "已修改", "已修改");
	}

	public static string OnSale()
	{
		return Pick("On sale", "促销中", "促銷中");
	}

	public static string Used()
	{
		return Pick("Used", "已使用", "已使用");
	}

	public static string CardRemoval()
	{
		return Pick("Card removal", "删牌服务", "刪牌服務");
	}

	public static string ShopService()
	{
		return Pick("Shop service", "商店服务", "商店服務");
	}

	public static string CardRemovalDescription()
	{
		return Pick("Remove one card from the deck.", "从牌组中移除一张卡牌。", "從牌組中移除一張卡牌。");
	}

	public static string CardRemovalUsedDescription()
	{
		return Pick("This shop's card removal has already been used.", "本次商店的删牌服务已经使用过了。", "本次商店的刪牌服務已經使用過了。");
	}

	public static string FormatGoldAmount(int amount)
	{
		return Pick($"{amount} gold", $"{amount} 金币", $"{amount} 金幣");
	}

	public static string RewardAvailable()
	{
		return Pick("A reward is available.", "当前有一个奖励可供查看。", "目前有一個獎勵可供查看。");
	}

	public static string LocalizeTag(string tag)
	{
		if (MatchesLocalizedToken(tag, "Action", "Action", "操作", "操作"))
		{
			return Action();
		}
		if (MatchesLocalizedToken(tag, "Card", "Card", "卡牌", "卡牌"))
		{
			return Card();
		}
		if (MatchesLocalizedToken(tag, "Relic", "Relic", "遗物", "遺物"))
		{
			return Relic();
		}
		if (MatchesLocalizedToken(tag, "Gold", "Gold", "金币", "金幣"))
		{
			return Gold();
		}
		if (MatchesLocalizedToken(tag, "Potion", "Potion", "药水", "藥水"))
		{
			return Potion();
		}
		if (MatchesLocalizedToken(tag, "Proceed", "Proceed", "继续", "繼續"))
		{
			return Proceed();
		}
		if (MatchesLocalizedToken(tag, "Event", "Event", "事件", "事件"))
		{
			return Event();
		}
		if (MatchesLocalizedToken(tag, "Reward", "Reward", "奖励", "獎勵"))
		{
			return Reward();
		}
		return tag;
	}

	public static bool IsCardTag(string? tag)
	{
		return MatchesLocalizedToken(tag, "Card", "Card", "卡牌", "卡牌");
	}

	public static bool IsRelicTag(string? tag)
	{
		return MatchesLocalizedToken(tag, "Relic", "Relic", "遗物", "遺物");
	}

	public static bool IsProceedTag(string? tag)
	{
		return MatchesLocalizedToken(tag, "Proceed", "Proceed", "继续", "繼續");
	}

	public static string GetSnapshotKindDisplayName(PartyObserverChoiceSnapshotKind kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			PartyObserverChoiceSnapshotKind.Rewards => Pick("Rewards", "奖励", "獎勵"), 
			PartyObserverChoiceSnapshotKind.RelicSelection => RelicChoice(), 
			PartyObserverChoiceSnapshotKind.CardRewardSelection => CardReward(), 
			PartyObserverChoiceSnapshotKind.EventChoices => Event(), 
			PartyObserverChoiceSnapshotKind.MerchantInventory => Shop(), 
			_ => Unknown(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public unsafe static string GetNetScreenDisplayName(NetScreenType screenType)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (int)screenType switch
		{
			0 => Pick("No Screen", "无界面", "無畫面"), 
			1 => Pick("Room", "房间", "房間"), 
			2 => Pick("Map", "地图", "地圖"), 
			3 => Pick("Settings", "设置", "設定"), 
			4 => Pick("Compendium", "图鉴", "圖鑑"), 
			5 => Pick("Deck View", "牌组界面", "牌組畫面"), 
			6 => Pick("Card Pile", "牌堆界面", "牌堆畫面"), 
			7 => Pick("Cards View", "卡牌查看", "卡牌檢視"), 
			8 => Pick("Card Selection", "卡牌选择", "卡牌選擇"), 
			9 => Pick("Game Over", "游戏结束", "遊戲結束"), 
			10 => Pick("Pause Menu", "暂停菜单", "暫停選單"), 
			11 => Pick("Rewards", "奖励", "獎勵"), 
			12 => Pick("Feedback", "反馈", "回饋"), 
			13 => Pick("Shared Relic Pick", "共享遗物选择", "共享遺物選擇"), 
			14 => Pick("Remote Player", "队友状态页", "隊友狀態頁"), 
			_ => ((object)(*(NetScreenType*)(&screenType))/*cast due to .constrained prefix*/).ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetCardType(CardType type)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (type - 1) switch
		{
			0 => Pick("Attack", "攻击", "攻擊"), 
			1 => Pick("Skill", "技能", "技能"), 
			2 => Pick("Power", "能力", "能力"), 
			3 => Pick("Status", "状态", "狀態"), 
			4 => Pick("Curse", "诅咒", "詛咒"), 
			5 => Pick("Quest", "任务", "任務"), 
			_ => Pick("None", "无", "無"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetCardRarity(CardRarity rarity)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (rarity - 1) switch
		{
			0 => Pick("Basic", "基础", "基礎"), 
			1 => Pick("Common", "普通", "普通"), 
			2 => Pick("Uncommon", "罕见", "罕見"), 
			3 => Pick("Rare", "稀有", "稀有"), 
			8 => Pick("Curse", "诅咒", "詛咒"), 
			7 => Pick("Status", "状态", "狀態"), 
			5 => Pick("Event", "事件", "事件"), 
			9 => Pick("Quest", "任务", "任務"), 
			4 => Pick("Ancient", "远古", "遠古"), 
			_ => Pick("None", "无", "無"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetRelicRarity(RelicRarity rarity)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (rarity - 1) switch
		{
			1 => Pick("Common", "普通", "普通"), 
			2 => Pick("Uncommon", "罕见", "罕見"), 
			3 => Pick("Rare", "稀有", "稀有"), 
			4 => Pick("Shop", "商店", "商店"), 
			0 => Pick("Starter", "初始", "初始"), 
			5 => Pick("Event", "事件", "事件"), 
			6 => Pick("Ancient", "远古", "遠古"), 
			_ => Pick("None", "无", "無"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string GetPotionRarity(PotionRarity rarity)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (rarity - 1) switch
		{
			0 => Pick("Common", "普通", "普通"), 
			1 => Pick("Uncommon", "罕见", "罕見"), 
			2 => Pick("Rare", "稀有", "稀有"), 
			_ => Pick("None", "无", "無"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string Pick(string english, string chineseSimplified, string chineseTraditional)
	{
		string text = CurrentLanguageToken();
		if (1 == 0)
		{
		}
		string result = ((text == "zhs") ? chineseSimplified : ((!(text == "zht")) ? english : chineseTraditional));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string NormalizeLanguage(string? language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return "eng";
		}
		string text = language.Trim().ToLowerInvariant();
		if (text.StartsWith("zhs", StringComparison.Ordinal) || text.StartsWith("zh-cn", StringComparison.Ordinal) || text.StartsWith("zh-hans", StringComparison.Ordinal) || text.StartsWith("chs", StringComparison.Ordinal))
		{
			return "zhs";
		}
		if (text.StartsWith("zht", StringComparison.Ordinal) || text.StartsWith("zh-tw", StringComparison.Ordinal) || text.StartsWith("zh-hk", StringComparison.Ordinal) || text.StartsWith("zh-hant", StringComparison.Ordinal) || text.StartsWith("cht", StringComparison.Ordinal))
		{
			return "zht";
		}
		return "eng";
	}

	private static bool MatchesLocalizedToken(string? value, string key, string english, string chineseSimplified, string chineseTraditional)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		return value.Equals(key, StringComparison.OrdinalIgnoreCase) || value.Equals(english, StringComparison.OrdinalIgnoreCase) || value.Equals(chineseSimplified, StringComparison.OrdinalIgnoreCase) || value.Equals(chineseTraditional, StringComparison.OrdinalIgnoreCase);
	}
}
