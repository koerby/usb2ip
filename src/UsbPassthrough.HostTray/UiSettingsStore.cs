using System.IO;
using System.Text.Json;
using ModernWpf;

namespace UsbPassthrough.HostTray;

public sealed record UiSettings(string Theme = "System");

public sealed class UiSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public UiSettingsStore()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UsbPassthrough");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "ui.json");
    }

    public UiSettings Load()
    {
        if (!File.Exists(_path))
        {
            var defaults = new UiSettings();
            Save(defaults);
            return defaults;
        }

        return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(_path)) ?? new UiSettings();
    }

    public void Save(UiSettings settings)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void ApplyTheme(string value)
    {
        ThemeManager.Current.ApplicationTheme = value switch
        {
            "Dark" => ApplicationTheme.Dark,
            "Light" => ApplicationTheme.Light,
            _ => null,
        };
    }
}