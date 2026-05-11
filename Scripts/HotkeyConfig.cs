using Godot;
using MegaCrit.Sts2.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace STS2Advisor.Scripts;

internal enum HotkeyAction
{
    AdvisorPanelToggle,
    EventAdvisorPanelToggle,
    ShaNagbaImuruToggle,
    GrandOrderToggleDetails,
}

internal sealed class HotkeyConfigData
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, string> Hotkeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class HotkeyConfig
{
    private const string FileName = "sts-2-advisor-hotkeys.json";
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly HotkeyConfigData DefaultData = new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Hotkeys = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AdvisorPanelToggle"] = "F3",
            ["EventAdvisorPanelToggle"] = "F1",
            ["ShaNagbaImuruToggle"] = "F4",
            ["GrandOrderToggleDetails"] = "F2",
        }
    };

    private static string GetConfigPath()
    {
        string dir = OS.GetUserDataDir();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.GetTempPath();

        dir = Path.Combine(dir, "mods", "sts-2-advisor");
        return Path.Combine(dir, FileName);
    }

    private static HotkeyConfigData LoadOrCreate()
    {
        string path = GetConfigPath();
        try
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Save(DefaultData);
                return DefaultData;
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<HotkeyConfigData>(json, JsonOptions);
            if (data == null || data.SchemaVersion != CurrentSchemaVersion)
                return DefaultData;

            data.Hotkeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return data;
        }
        catch (Exception e)
        {
            Log.Error($"[HotkeyConfig] Load failed, using default: {e}");
            return DefaultData;
        }
    }

    private static void Save(HotkeyConfigData data)
    {
        string path = GetConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static readonly HotkeyConfigData Data = LoadOrCreate();

    private static string GetToken(HotkeyAction action)
    {
        string key = action.ToString();
        if (Data.Hotkeys.TryGetValue(key, out var token) && !string.IsNullOrWhiteSpace(token))
            return token.Trim();

        // Fallback to defaults
        return DefaultData.Hotkeys.TryGetValue(key, out var def) ? def : "F1";
    }

    internal static Key GetKey(HotkeyAction action)
    {
        string token = GetToken(action);
        if (token.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
            || token.Equals("None", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Off", StringComparison.OrdinalIgnoreCase)
            || token.Equals("-", StringComparison.OrdinalIgnoreCase)
            || token.Equals("",
                StringComparison.OrdinalIgnoreCase))
        {
            return Key.None;
        }

        return ParseKey(token, Key.None);
    }

    internal static string GetTokenText(HotkeyAction action)
        => GetToken(action);

    private static Key ParseKey(string token, Key fallback)
    {
        if (string.IsNullOrWhiteSpace(token)) return fallback;
        token = token.Trim().ToUpperInvariant();

        return token switch
        {
            "F1" => Key.F1,
            "F2" => Key.F2,
            "F3" => Key.F3,
            "F4" => Key.F4,
            "F5" => Key.F5,
            "F6" => Key.F6,
            "F7" => Key.F7,
            "F8" => Key.F8,
            "F9" => Key.F9,
            "F10" => Key.F10,
            "F11" => Key.F11,
            "F12" => Key.F12,
            _ => fallback,
        };
    }
}

