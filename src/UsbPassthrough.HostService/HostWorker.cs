using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UsbPassthrough.Backend;

namespace UsbPassthrough.HostService;

public sealed class HostWorker(ILogger<HostWorker> logger, IUsbBackend backend, HostStateStore stateStore, ConfigStore configStore) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stateStore.Log("Information", "Host worker started");

        _ = configStore.Load();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _ = await backend.GetDriverStatus(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Driver status check failed");
                stateStore.Log("Warning", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}