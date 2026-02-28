using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UsbPassthrough.Backend;
using UsbPassthrough.Common;

namespace UsbPassthrough.GuestAgent;

public sealed class GuestWorker(ILogger<GuestWorker> logger, IUsbBackend backend) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UsbPassthrough", "guest.json");
    private readonly List<string> _attached = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = LoadConfig();
        var retry = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (config.AutoReconnect)
                {
                    foreach (var device in _attached.ToArray())
                    {
                        _ = await backend.AttachOnGuest(config.HostAddress, device, stoppingToken);
                    }
                }

                retry = 0;
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (Exception ex)
            {
                retry++;
                var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retry)));
                logger.LogWarning(ex, "Guest reconnect failed, retry in {Delay}s", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var device in _attached)
        {
            _ = await backend.DetachOnGuest(device, cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    private GuestConfig LoadConfig()
    {
        var root = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(root);
        if (!File.Exists(_configPath))
        {
            var config = new GuestConfig();
            File.WriteAllText(_configPath, JsonSerializer.Serialize(config, JsonOptions));
            return config;
        }

        return JsonSerializer.Deserialize<GuestConfig>(File.ReadAllText(_configPath), JsonOptions) ?? new GuestConfig();
    }
}