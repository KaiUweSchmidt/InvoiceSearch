using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceSearch.Services;

/// <summary>
/// Manages application settings persisted in app-settings.json.
/// </summary>
public static class AppSettingsService
{
    private static readonly string s_settingsPath;

    static AppSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InvoiceSearch");
        Directory.CreateDirectory(folder);
        s_settingsPath = Path.Combine(folder, "app-settings.json");
    }

    /// <summary>
    /// Loads the persisted settings, or returns defaults if the file does not exist.
    /// </summary>
    public static AppSettings Load()
    {
        if (!File.Exists(s_settingsPath))
            return new AppSettings();

        var json = File.ReadAllText(s_settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    /// <summary>
    /// Persists the settings to disk.
    /// </summary>
    public static void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(s_settingsPath, json);
    }
}

/// <summary>
/// Application-wide settings.
/// </summary>
public sealed record AppSettings
{
    [JsonPropertyName("exportPath")]
    public string? ExportPath { get; init; }
}
