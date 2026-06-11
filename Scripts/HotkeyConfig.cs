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
        // Prefer writable file next to the installed mod for easy user edits.
        string? gameDir = Path.GetDirectoryName(OS.GetExecutablePath());
        if (!string.IsNullOrWhiteSpace(gameDir))
        {
            string modsPath = Path.Combine(gameDir, "mods", "STS2Advisor", FileName);
            return modsPath;
        }

        // Fallback to user:// when executable path is unavailable.
        string dir = OS.GetUserDataDir();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.GetTempPath();
        return Path.Combine(dir, "mods", "sts-2-advisor", FileName);
    }

    private static string GetLegacyUserConfigPath()
    {
        string dir = OS.GetUserDataDir();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Path.GetTempPath();
        return Path.Combine(dir, "mods", "sts-2-advisor", FileName);
    }

    private static HotkeyConfigData LoadOrCreate()
    {
        string path = GetConfigPath();
        try
        {
            // One-time migration from legacy user:// path into mod folder.
            if (!File.Exists(path))
            {
                string legacyPath = GetLegacyUserConfigPath();
                if (File.Exists(legacyPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.Copy(legacyPath, path, overwrite: true);
                }
            }

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
        token = token.Trim();

        if (Enum.TryParse<Key>(token, ignoreCase: true, out var parsed))
            return parsed;

        return fallback;
    }
}