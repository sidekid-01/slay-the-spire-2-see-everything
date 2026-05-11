using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2Advisor.Scripts;

internal static class LocText
{
	public static string Of(object? obj)
	{
		if (obj == null)
		{
			return string.Empty;
		}

		if (obj is CardModel card)
		{
			if (!string.IsNullOrWhiteSpace(card.Title))
			{
				return card.Title.Trim();
			}

			string title = ResolveLocString(card.TitleLocString, card.DynamicVars);
			if (!string.IsNullOrWhiteSpace(title))
			{
				return title;
			}
		}

		var titleProp = obj.GetType().GetProperty("Title");
		if (titleProp?.GetValue(obj) is LocString ls)
		{
			string text = ResolveLocString(ls, null);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}

		var idProp = obj.GetType().GetProperty("Id");
		var id = idProp?.GetValue(obj);
		var entry = id?.GetType().GetProperty("Entry")?.GetValue(id) as string;
		return string.IsNullOrWhiteSpace(entry) ? (obj.ToString() ?? string.Empty) : entry;
	}

	private static string ResolveLocString(LocString? locString, DynamicVarSet? dynamicVars)
	{
		if (LocString.IsNullOrWhitespace(locString))
		{
			return string.Empty;
		}

		Dictionary<string, object> vars = new(StringComparer.OrdinalIgnoreCase);
		if (locString?.Variables != null)
		{
			foreach (KeyValuePair<string, object> variable in locString.Variables)
			{
				if (!string.IsNullOrWhiteSpace(variable.Key) && variable.Value != null)
				{
					vars[variable.Key] = variable.Value;
				}
			}
		}

		if (dynamicVars != null)
		{
			foreach (KeyValuePair<string, DynamicVar> dynamicVar in dynamicVars)
			{
				if (!string.IsNullOrWhiteSpace(dynamicVar.Key))
				{
					vars[dynamicVar.Key] = dynamicVar.Value;
				}
			}
		}

		try
		{
			var loc = LocManager.Instance;
			if (loc != null && vars.Count > 0)
			{
				return loc.SmartFormat(locString!, vars);
			}
		}
		catch
		{
		}

		try
		{
			return locString!.GetFormattedText();
		}
		catch
		{
			try
			{
				return locString!.GetRawText();
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}

