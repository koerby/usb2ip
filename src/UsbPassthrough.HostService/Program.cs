using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using UsbPassthrough.Backend;
using UsbPassthrough.Common;
using UsbPassthrough.HostService;

var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UsbPassthrough");
Directory.CreateDirectory(dataRoot);
Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(dataRoot, "logs", "host-.log"), rollingInterval: RollingInterval.Day)
    .WriteTo.EventLog("UsbPassthrough.HostService", manageEventSource: true)
    .CreateLogger();

var hostBuilder = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices(services =>
    {
        if (WindowsServiceHelpers.IsWindowsService())
        {
            services.AddWindowsService();
        }

        services.AddSingleton<IUsbBackend, UsbBackend>();
        services.AddSingleton<HostStateStore>();
        services.AddSingleton<ConfigStore>();
        services.AddHostedService<HostWorker>();
        services.AddHostedService<PipeServerService>();
    });

await hostBuilder.Build().RunAsync();