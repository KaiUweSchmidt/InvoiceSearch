using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace InvoiceSearch.Services;

/// <summary>
/// Saves and restores window position and size across sessions.
/// </summary>
public static class WindowPlacementService
{
    private static readonly string s_placementPath;

    static WindowPlacementService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InvoiceSearch");
        Directory.CreateDirectory(folder);
        s_placementPath = Path.Combine(folder, "window-placement.json");
    }

    /// <summary>
    /// Restores window position and size from the persisted file.
    /// </summary>
    public static void Restore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!File.Exists(s_placementPath))
            return;

        try
        {
            var json = File.ReadAllText(s_placementPath);
            var placement = JsonSerializer.Deserialize<WindowPlacement>(json);
            if (placement is null) return;

            window.Left = placement.Left;
            window.Top = placement.Top;
            window.Width = placement.Width;
            window.Height = placement.Height;

            if (placement.IsMaximized)
                window.WindowState = WindowState.Maximized;
        }
        catch
        {
            // Ignore corrupt placement files
        }
    }

    /// <summary>
    /// Persists the current window position and size to disk.
    /// </summary>
    public static void Save(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var placement = new WindowPlacement
        {
            Left = window.RestoreBounds.Left,
            Top = window.RestoreBounds.Top,
            Width = window.RestoreBounds.Width,
            Height = window.RestoreBounds.Height,
            IsMaximized = window.WindowState == WindowState.Maximized
        };

        var json = JsonSerializer.Serialize(placement, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(s_placementPath, json);
    }
}

/// <summary>
/// Serializable window placement data.
/// </summary>
public sealed record WindowPlacement
{
    [JsonPropertyName("left")]
    public double Left { get; init; }

    [JsonPropertyName("top")]
    public double Top { get; init; }

    [JsonPropertyName("width")]
    public double Width { get; init; }

    [JsonPropertyName("height")]
    public double Height { get; init; }

    [JsonPropertyName("isMaximized")]
    public bool IsMaximized { get; init; }
}
