using System;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace STS2Advisor.Scripts;

internal static class STS2AdvisorI18n
{
	private const string DefaultLanguage = "en";

	public static string CurrentLanguageToken()
	{
		string? lang = null;
		try
		{
			lang = LocManager.Instance?.Language;
		}
		catch
		{
		}

		if (string.IsNullOrWhiteSpace(lang))
		{
			try
			{
				lang = TranslationServer.GetLocale();
			}
			catch
			{
			}
		}

		return NormalizeLanguageCode(lang);
	}

	public static bool IsChineseSimplified()
	{
		return string.Equals(CurrentLanguageToken(), "zhs", StringComparison.OrdinalIgnoreCase);
	}

	public static string Pick(string english, string chineseSimplified)
	{
		return IsChineseSimplified() ? chineseSimplified : english;
	}

	private static string NormalizeLanguageCode(string? language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return DefaultLanguage;
		}

		string text = language.Trim().Replace('-', '_').ToLowerInvariant();
		if (text.StartsWith("zh", StringComparison.Ordinal)
			|| text.StartsWith("zhs", StringComparison.Ordinal)
			|| text.StartsWith("chs", StringComparison.Ordinal)
			|| text.StartsWith("chi", StringComparison.Ordinal))
		{
			return "zhs";
		}

		if (text.StartsWith("en", StringComparison.Ordinal)
			|| text.StartsWith("eng", StringComparison.Ordinal))
		{
			return "en";
		}

		return text;
	}
}

