using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace PartyObserver.Services;

internal static class PartyObserverSettingsStore
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private static PartyObserverSettings? _cached;

	public static PartyObserverSettings Load()
	{
		if (_cached != null)
		{
			return _cached;
		}
		string settingsPath = GetSettingsPath();
		string directoryName = Path.GetDirectoryName(settingsPath);
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		try
		{
			if (!File.Exists(settingsPath))
			{
				_cached = new PartyObserverSettings();
				Save(_cached);
				return _cached;
			}
			string json = File.ReadAllText(settingsPath);
			_cached = JsonSerializer.Deserialize<PartyObserverSettings>(json, JsonOptions) ?? new PartyObserverSettings();
			_cached.Normalize();
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"PartyObserver"}: failed to load settings, using defaults: {value}");
			_cached = new PartyObserverSettings();
		}
		return _cached;
	}

	public static void Save(PartyObserverSettings settings)
	{
		settings.Normalize();
		_cached = settings;
		string contents = JsonSerializer.Serialize(settings, JsonOptions);
		File.WriteAllText(GetSettingsPath(), contents);
	}

	private static string GetSettingsPath()
	{
		return Path.Combine(AppContext.BaseDirectory, "mods", "PartyObserver", "partyobserver.settings.json");
	}
}
