using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceSearch.Services;

/// <summary>
/// Manages application settings persisted in app-settings.json.
/// </summary>
public sealed class AppSettingsService
{
    private readonly string _settingsPath;

    public AppSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InvoiceSearch");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "app-settings.json");
    }

    /// <summary>
    /// Creates a service using a custom settings file path (for testing).
    /// </summary>
    public AppSettingsService(string settingsPath)
    {
        ArgumentNullException.ThrowIfNull(settingsPath);
        _settingsPath = settingsPath;
    }

    /// <summary>
    /// Loads the persisted settings, or returns defaults if the file does not exist.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    /// <summary>
    /// Persists the settings to disk.
    /// </summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
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
