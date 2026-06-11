using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace STS2Advisor.Scripts;

/// <summary>
/// Permanent signature badge on screen bottom-left.
/// Non-interactive, non-draggable, and always visible.
/// </summary>
public partial class SignatureBadgeOverlay : CanvasLayer
{
    private const string DefaultBadgeText = "聚以天上繁星之吐息，辉煌生命之奔流！Excalibur";
    private const int MinSubtitleLength = 1;
    private const int MaxSubtitleLength = 50;
    private const float BaseOffsetLeft = 660f;
    private const float DragRangeRight = 500f;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private sealed class SignatureBadgeJson
    {
        public string Subtitle { get; set; } = DefaultBadgeText;
    }

    private PanelContainer? _root;
    private RichTextLabel? _text;
    private RichTextLabel? _glowText;
    private RichTextLabel? _trailText1;
    private RichTextLabel? _trailText2;
    private string _badgeText = DefaultBadgeText;
    private float _time;
    private bool _dragging;
    private float _dragOffsetX;

    public override void _Ready()
    {
        Layer = 120;
        _badgeText = LoadOrCreateSubtitle();

        _root = new PanelContainer();
        _root.Name = "signature_badge_overlay";
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        _root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _root.OffsetLeft = BaseOffsetLeft;
        _root.OffsetTop = 10;
        _root.OffsetRight = BaseOffsetLeft + 500f;
        _root.OffsetBottom = 46;
        _root.ClipContents = true;
        _root.AddThemeStyleboxOverride("panel", MakeBadgeStyle());
        AddChild(_root);

        var pad = new MarginContainer();
        pad.MouseFilter = Control.MouseFilterEnum.Ignore;
        pad.AddThemeConstantOverride("margin_left", 10);
        pad.AddThemeConstantOverride("margin_right", 10);
        pad.AddThemeConstantOverride("margin_top", 6);
        pad.AddThemeConstantOverride("margin_bottom", 6);
        _root.AddChild(pad);

        var overlay = new Control();
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        overlay.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        overlay.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        overlay.CustomMinimumSize = new Vector2(480, 26);
        overlay.ClipContents = true;
        pad.AddChild(overlay);

        _glowText = new RichTextLabel();
        _glowText.MouseFilter = Control.MouseFilterEnum.Ignore;
        _glowText.BbcodeEnabled = true;
        _glowText.FitContent = true;
        _glowText.ScrollActive = false;
        _glowText.AutowrapMode = TextServer.AutowrapMode.Off;
        _glowText.AddThemeFontSizeOverride("normal_font_size", 15);
        _glowText.HorizontalAlignment = HorizontalAlignment.Center;
        _glowText.Position = new Vector2(1, 1);
        overlay.AddChild(_glowText);

        _trailText1 = new RichTextLabel();
        _trailText1.MouseFilter = Control.MouseFilterEnum.Ignore;
        _trailText1.BbcodeEnabled = true;
        _trailText1.FitContent = true;
        _trailText1.ScrollActive = false;
        _trailText1.AutowrapMode = TextServer.AutowrapMode.Off;
        _trailText1.AddThemeFontSizeOverride("normal_font_size", 15);
        _trailText1.HorizontalAlignment = HorizontalAlignment.Center;
        _trailText1.Position = new Vector2(3, 2);
        overlay.AddChild(_trailText1);

        _trailText2 = new RichTextLabel();
        _trailText2.MouseFilter = Control.MouseFilterEnum.Ignore;
        _trailText2.BbcodeEnabled = true;
        _trailText2.FitContent = true;
        _trailText2.ScrollActive = false;
        _trailText2.AutowrapMode = TextServer.AutowrapMode.Off;
        _trailText2.AddThemeFontSizeOverride("normal_font_size", 15);
        _trailText2.HorizontalAlignment = HorizontalAlignment.Center;
        _trailText2.Position = new Vector2(6, 3);
        overlay.AddChild(_trailText2);

        _text = new RichTextLabel();
        _text.MouseFilter = Control.MouseFilterEnum.Ignore;
        _text.BbcodeEnabled = true;
        _text.FitContent = true;
        _text.ScrollActive = false;
        _text.AutowrapMode = TextServer.AutowrapMode.Off;
        _text.AddThemeFontSizeOverride("normal_font_size", 15);
        _text.HorizontalAlignment = HorizontalAlignment.Center;
        overlay.AddChild(_text);

        UpdateLavaText();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        UpdateLavaText();
    }

    public override void _Input(InputEvent evt)
    {
        if (_root == null || !_root.Visible)
            return;

        if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                Vector2 mouse = GetViewport().GetMousePosition();
                if (_root.GetGlobalRect().HasPoint(mouse))
                {
                    _dragging = true;
                    _dragOffsetX = _root.Position.X - mouse.X;
                }
            }
            else
            {
                _dragging = false;
            }
        }

        if (_dragging && evt is InputEventMouseMotion)
        {
            Vector2 mouse = GetViewport().GetMousePosition();
            float width = _root.Size.X > 0 ? _root.Size.X : (_root.OffsetRight - _root.OffsetLeft);
            float x = mouse.X + _dragOffsetX;
            x = Mathf.Clamp(x, BaseOffsetLeft, BaseOffsetLeft + DragRangeRight);

            _root.OffsetLeft = x;
            _root.OffsetRight = x + width;
        }
    }

    private void UpdateLavaText()
    {
        if (_text == null) return;

        // Noble gold-red pulse: dark red-gold base with bright golden highlights.
        float t1 = 0.5f + 0.5f * Mathf.Sin(_time * 2.2f);
        float t2 = 0.5f + 0.5f * Mathf.Sin(_time * 5.4f + 0.8f);
        float shine = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(_time * 7.8f), 3.0f);

        Color darkGold = new Color(0.62f, 0.22f, 0.09f, 1f);
        Color hotGold = new Color(0.92f, 0.63f, 0.16f, 1f);
        Color brightGold = new Color(1.00f, 0.88f, 0.52f, 1f);

        Color baseColor = darkGold.Lerp(hotGold, t1);
        Color glowColor = hotGold.Lerp(brightGold, 0.35f * t2 + 0.65f * shine);
        Color sparkColor = brightGold.Lerp(new Color(1.0f, 0.98f, 0.85f, 1f), shine);

        string c1 = ToHex(baseColor);
        string c2 = ToHex(glowColor);
        string c3 = ToHex(sparkColor);

        _text.Text = BuildStyledText(_badgeText, c1, c2, c3);

        if (_glowText != null)
        {
            // Soft crimson glow underlayer to enhance pressure/aura.
            float glowPulse = 0.24f + 0.20f * (0.5f + 0.5f * Mathf.Sin(_time * 4.2f));
            string glowHex = ToHex(new Color(0.95f, 0.22f, 0.16f, 1f));
            _glowText.Text = $"[color={glowHex}]{_badgeText}[/color]";
            _glowText.Modulate = new Color(1f, 1f, 1f, glowPulse);
        }

        if (_trailText1 != null)
        {
            float trailPulse1 = 0.30f + 0.16f * (0.5f + 0.5f * Mathf.Sin(_time * 3.6f + 0.5f));
            string trailHex1 = ToHex(new Color(0.85f, 0.12f, 0.10f, 1f));
            _trailText1.Text = $"[color={trailHex1}]{_badgeText}[/color]";
            _trailText1.Modulate = new Color(1f, 1f, 1f, trailPulse1);
        }

        if (_trailText2 != null)
        {
            float trailPulse2 = 0.18f + 0.14f * (0.5f + 0.5f * Mathf.Sin(_time * 2.9f + 1.2f));
            string trailHex2 = ToHex(new Color(0.62f, 0.05f, 0.06f, 1f));
            _trailText2.Text = $"[color={trailHex2}]{_badgeText}[/color]";
            _trailText2.Modulate = new Color(1f, 1f, 1f, trailPulse2);
        }
    }

    private static string BuildStyledText(string text, string c1, string c2, string c3)
    {
        if (string.IsNullOrWhiteSpace(text))
            text = DefaultBadgeText;

        int total = text.Length;
        int part1 = Math.Max(1, total / 3);
        int part2 = Math.Max(1, (total - part1) / 2);
        int part3 = total - part1 - part2;
        if (part3 <= 0)
        {
            part3 = 1;
            if (part2 > 1) part2--;
            else if (part1 > 1) part1--;
        }

        string s1 = text[..part1];
        string s2 = text.Substring(part1, part2);
        string s3 = text.Substring(part1 + part2, part3);
        return $"[color={c1}]{s1}[/color][color={c2}]{s2}[/color][color={c3}]{s3}[/color]";
    }

    private static string LoadOrCreateSubtitle()
    {
        try
        {
            string path = GetPreferredBadgeConfigPath();
            string legacyPath = ProjectSettings.GlobalizePath("user://mods/sts-2-advisor/signature_badge.json");
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // One-time migration from legacy user:// path.
            if (!File.Exists(path) && File.Exists(legacyPath))
                File.Copy(legacyPath, path, overwrite: true);

            if (!File.Exists(path))
            {
                var created = new SignatureBadgeJson { Subtitle = DefaultBadgeText };
                File.WriteAllText(path, JsonSerializer.Serialize(created, JsonOptions));
                return created.Subtitle;
            }

            var data = JsonSerializer.Deserialize<SignatureBadgeJson>(File.ReadAllText(path), JsonOptions);
            string subtitle = data?.Subtitle ?? DefaultBadgeText;
            if (string.IsNullOrWhiteSpace(subtitle))
                subtitle = DefaultBadgeText;
            subtitle = subtitle.Trim();

            // Clamp to 1..50 chars by requirement.
            if (subtitle.Length < MinSubtitleLength)
                subtitle = DefaultBadgeText;
            if (subtitle.Length > MaxSubtitleLength)
                subtitle = subtitle[..MaxSubtitleLength];

            return subtitle;
        }
        catch
        {
            return DefaultBadgeText;
        }
    }

    private static string GetPreferredBadgeConfigPath()
    {
        string? gameDir = Path.GetDirectoryName(OS.GetExecutablePath());
        if (!string.IsNullOrWhiteSpace(gameDir))
            return Path.Combine(gameDir, "mods", "STS2Advisor", "signature_badge.json");

        return ProjectSettings.GlobalizePath("user://mods/sts-2-advisor/signature_badge.json");
    }

    private static string ToHex(Color c)
    {
        int r = Mathf.Clamp((int)(c.R * 255f), 0, 255);
        int g = Mathf.Clamp((int)(c.G * 255f), 0, 255);
        int b = Mathf.Clamp((int)(c.B * 255f), 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static StyleBoxFlat MakeBadgeStyle()
    {
        var s = new StyleBoxFlat();
        // Fully transparent panel (no frame/no gold box), text only.
        s.BgColor = new Color(0f, 0f, 0f, 0f);
        s.SetCornerRadiusAll(0);
        s.SetBorderWidthAll(0);
        s.BorderColor = new Color(0f, 0f, 0f, 0f);
        s.ShadowColor = new Color(0f, 0f, 0f, 0f);
        s.ShadowSize = 0;
        s.ShadowOffset = Vector2.Zero;
        return s;
    }
}
