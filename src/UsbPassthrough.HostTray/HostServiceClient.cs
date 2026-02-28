using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using UsbPassthrough.Common;

namespace UsbPassthrough.HostTray;

public sealed class HostServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<HostResponse> SendAsync(string command, object? payload = null, CancellationToken cancellationToken = default)
    {
        using var client = new NamedPipeClientStream(".", PipeNames.HostService, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(1000, cancellationToken);

        using var writer = new StreamWriter(client) { AutoFlush = true };
        using var reader = new StreamReader(client);

        var node = payload is null ? null : JsonSerializer.SerializeToNode(payload, JsonOptions) as JsonObject;
        var request = new HostRequest(command, node);
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));

        var responseLine = await reader.ReadLineAsync(cancellationToken) ?? "{\"success\":false,\"error\":\"No response\"}";
        return JsonSerializer.Deserialize<HostResponse>(responseLine, JsonOptions) ?? new HostResponse(false, "Parse error");
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string command, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(command, cancellationToken: cancellationToken);
        if (!response.Success || response.Payload is null)
        {
            return [];
        }

        if (response.Payload["data"] is JsonNode data)
        {
            return data.Deserialize<List<T>>(JsonOptions) ?? [];
        }

        return response.Payload.Deserialize<List<T>>(JsonOptions) ?? [];
    }
}