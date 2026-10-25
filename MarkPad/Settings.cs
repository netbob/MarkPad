using System;
using System.IO;
using System.Text.Json;

namespace MarkPad;

/// <summary>
/// The only things MarkPad remembers: the theme and the last folder used.
/// Stored as plain JSON under %APPDATA%\MarkPad\settings.json.
/// </summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string LastFolder { get; set; } = string.Empty;

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkPad",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults. Best effort only.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings are best effort; a failed save must never break editing.
        }
    }
}
