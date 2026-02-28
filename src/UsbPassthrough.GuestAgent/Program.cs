using Microsoft.Extensions.Hosting.WindowsServices;
using UsbPassthrough.Backend;
using UsbPassthrough.GuestAgent;

var builder = Host.CreateApplicationBuilder(args);
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Services.AddWindowsService();
}

builder.Services.AddSingleton<IUsbBackend, UsbBackend>();
builder.Services.AddHostedService<GuestWorker>();

await builder.Build().RunAsync();