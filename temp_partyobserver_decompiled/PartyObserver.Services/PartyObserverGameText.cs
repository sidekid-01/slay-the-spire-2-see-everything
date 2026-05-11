using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace PartyObserver.Services;

internal static class PartyObserverGameText
{
	public static string ResolveCardTitle(CardModel card)
	{
		return (!string.IsNullOrWhiteSpace(card.Title)) ? card.Title : ResolveLocString(card.TitleLocString, card.DynamicVars);
	}

	public static string ResolveCardDescription(CardModel card)
	{
		try
		{
			return card.GetDescriptionForPile((PileType)0, (Creature)null);
		}
		catch
		{
			return ResolveLocString(card.Description, card.DynamicVars);
		}
	}

	public static string ResolveCardImagePath(CardModel card)
	{
		return FirstNonEmpty(card.PortraitPath, card.AllPortraitPaths?.FirstOrDefault((string path) => !string.IsNullOrWhiteSpace(path))) ?? string.Empty;
	}

	public static string ResolveRelicTitle(RelicModel relic)
	{
		return ResolveLocString(relic.Title, relic.DynamicVars);
	}

	public static string ResolveRelicDescription(RelicModel relic)
	{
		return FirstNonEmpty(ResolveLocString(relic.DynamicDescription, relic.DynamicVars), ResolveLocString(relic.Description, relic.DynamicVars), ResolveLocString(relic.DynamicEventDescription, relic.DynamicVars)) ?? string.Empty;
	}

	public static string ResolveRelicImagePath(RelicModel relic)
	{
		return FirstNonEmpty(relic.IconPath, relic.PackedIconPath) ?? string.Empty;
	}

	public static string ResolvePotionTitle(PotionModel potion)
	{
		return ResolveLocString(potion.Title, potion.DynamicVars);
	}

	public static string ResolvePotionDescription(PotionModel potion)
	{
		return ResolveLocString(potion.DynamicDescription, potion.DynamicVars);
	}

	public static string ResolvePotionImagePath(PotionModel potion)
	{
		return FirstNonEmpty(potion.ImagePath, potion.OutlinePath) ?? string.Empty;
	}

	public static string ResolveLocString(LocString locString, DynamicVarSet? dynamicVars = null)
	{
		if (LocString.IsNullOrWhitespace(locString))
		{
			return string.Empty;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		if (locString.Variables != null)
		{
			foreach (KeyValuePair<string, object> variable in locString.Variables)
			{
				if (!string.IsNullOrWhiteSpace(variable.Key) && variable.Value != null)
				{
					dictionary[variable.Key] = variable.Value;
				}
			}
		}
		if (dynamicVars != null)
		{
			foreach (KeyValuePair<string, DynamicVar> dynamicVar in dynamicVars)
			{
				if (!string.IsNullOrWhiteSpace(dynamicVar.Key))
				{
					dictionary[dynamicVar.Key] = dynamicVar.Value;
				}
			}
		}
		if (LocManager.Instance != null)
		{
			try
			{
				if (dictionary.Count > 0)
				{
					return LocManager.Instance.SmartFormat(locString, dictionary);
				}
			}
			catch
			{
			}
		}
		try
		{
			return locString.GetFormattedText();
		}
		catch
		{
			try
			{
				return locString.GetRawText();
			}
			catch
			{
				return string.Empty;
			}
		}
	}

	private static string? FirstNonEmpty(params string?[] candidates)
	{
		foreach (string text in candidates)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return null;
	}
}
