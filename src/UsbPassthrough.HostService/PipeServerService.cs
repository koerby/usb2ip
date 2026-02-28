using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UsbPassthrough.Backend;
using UsbPassthrough.Common;

namespace UsbPassthrough.HostService;

public sealed class PipeServerService(ILogger<PipeServerService> logger, IUsbBackend backend, HostStateStore stateStore, ConfigStore configStore) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stateStore.Log("Information", "Named-pipe IPC online");
        while (!stoppingToken.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(PipeNames.HostService, PipeDirection.InOut, 5, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleClient(server, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "IPC request failed");
                    stateStore.Log("Error", ex.Message);
                }
            }, stoppingToken);
        }
    }

    private async Task HandleClient(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        var line = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var request = JsonSerializer.Deserialize<HostRequest>(line, JsonOptions);
        if (request is null)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(new HostResponse(false, "Invalid request"), JsonOptions));
            return;
        }

        var response = await Dispatch(request, cancellationToken);
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private async Task<HostResponse> Dispatch(HostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return request.Command switch
            {
                HostCommands.GetServiceStatus => Ok(new ServiceStatusDto(true, "Running", DateTimeOffset.UtcNow)),
                HostCommands.ListUsbDevices => Ok(await backend.ListDevices(cancellationToken)),
                HostCommands.GetAttachments => Ok(stateStore.GetAttachments()),
                HostCommands.GetRecentLogs => HandleRecentLogs(request),
                HostCommands.PatchConfig => HandlePatchConfig(request),
                HostCommands.AttachDevice => await HandleAttach(request, cancellationToken),
                HostCommands.DetachDevice => await HandleDetach(request, cancellationToken),
                HostCommands.GetDiagnostics => Ok(await BuildDiagnostics(cancellationToken)),
                _ => new HostResponse(false, $"Unknown command '{request.Command}'")
            };
        }
        catch (Exception ex)
        {
            stateStore.Log("Error", ex.Message);
            return new HostResponse(false, ex.Message);
        }
    }

    private HostResponse HandleRecentLogs(HostRequest request)
    {
        var max = request.Payload?["max"]?.GetValue<int>() ?? 200;
        return Ok(stateStore.RecentLogs(max));
    }

    private HostResponse HandlePatchConfig(HostRequest request)
    {
        var patch = request.Payload?.Deserialize<PatchConfigRequest>(JsonOptions) ?? new PatchConfigRequest(null, null, null, null, null, null);
        var config = configStore.Patch(patch);
        stateStore.Log("Information", "Config updated");
        return Ok(config);
    }

    private async Task<HostResponse> HandleAttach(HostRequest request, CancellationToken cancellationToken)
    {
        var attach = request.Payload?.Deserialize<AttachDeviceRequest>(JsonOptions);
        if (attach is null)
        {
            return new HostResponse(false, "attach payload missing");
        }

        var result = await backend.BindDevice(attach.DeviceId, cancellationToken);
        if (!result.Success)
        {
            return new HostResponse(false, result.Message ?? "Bind failed");
        }

        stateStore.AddAttachment(new AttachmentDto(attach.DeviceId, attach.DeviceId, attach.ClientAddress, DateTimeOffset.UtcNow, "Shared"));
        stateStore.Log("Information", $"Shared {attach.DeviceId}");
        return Ok(new { attached = true });
    }

    private async Task<HostResponse> HandleDetach(HostRequest request, CancellationToken cancellationToken)
    {
        var detach = request.Payload?.Deserialize<DetachDeviceRequest>(JsonOptions);
        if (detach is null)
        {
            return new HostResponse(false, "detach payload missing");
        }

        var result = await backend.UnbindDevice(detach.DeviceId, cancellationToken);
        if (!result.Success)
        {
            return new HostResponse(false, result.Message ?? "Unbind failed");
        }

        _ = stateStore.RemoveAttachment(detach.DeviceId);
        stateStore.Log("Information", $"Unshared {detach.DeviceId}");
        return Ok(new { detached = true });
    }

    private async Task<object> BuildDiagnostics(CancellationToken cancellationToken)
    {
        var driver = await backend.GetDriverStatus(cancellationToken);
        return new
        {
            Service = "Running",
            Driver = driver.Message,
            ToolPresent = driver.Installed,
            ClientReachable = true,
        };
    }

    private static HostResponse Ok<T>(T payload)
        => new(true, Payload: JsonSerializer.SerializeToNode(payload, JsonOptions) as JsonObject ?? new JsonObject { ["data"] = JsonSerializer.SerializeToNode(payload, JsonOptions) });
}