using System.Text.Json;
using UsbPassthrough.Common;

namespace UsbPassthrough.HostService;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _configPath;

    public ConfigStore()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UsbPassthrough");
        Directory.CreateDirectory(root);
        _configPath = Path.Combine(root, "config.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var created = new AppConfig();
            Save(created);
            return created;
        }

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public AppConfig Patch(PatchConfigRequest patch)
    {
        var current = Load();
        var updated = current with
        {
            StartWithWindows = patch.StartWithWindows ?? current.StartWithWindows,
            StartMinimizedToTray = patch.StartMinimizedToTray ?? current.StartMinimizedToTray,
            AutoReconnect = patch.AutoReconnect ?? current.AutoReconnect,
            Notifications = patch.Notifications ?? current.Notifications,
            Psk = patch.Psk ?? current.Psk,
            CertThumbprint = patch.CertThumbprint ?? current.CertThumbprint,
        };
        Save(updated);
        return updated;
    }

    public void Save(AppConfig config)
    {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}