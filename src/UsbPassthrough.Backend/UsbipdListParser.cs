using System.Text.RegularExpressions;
using UsbPassthrough.Common;

namespace UsbPassthrough.Backend;

public static partial class UsbipdListParser
{
    [GeneratedRegex("^\\s*(?<busid>\\S+)\\s+(?<vidpid>[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4})\\s+(?<name>.+?)\\s{2,}(?<state>.+)$")]
    private static partial Regex ConnectedRegex();

    public static IReadOnlyList<UsbDeviceDto> Parse(string output)
    {
        var devices = new List<UsbDeviceDto>();
        using var reader = new StringReader(output);
        string? line;
        var inConnected = false;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("Connected:", StringComparison.OrdinalIgnoreCase))
            {
                inConnected = true;
                continue;
            }
            if (!inConnected || string.IsNullOrWhiteSpace(line) || line.StartsWith("BUSID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = ConnectedRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var busId = match.Groups["busid"].Value;
            var vidpid = match.Groups["vidpid"].Value.ToUpperInvariant();
            var name = match.Groups["name"].Value.Trim();
            var state = match.Groups["state"].Value.Trim();

            var attachedVm = state.Contains("Attached", StringComparison.OrdinalIgnoreCase) ? "external-client" : null;
            devices.Add(new UsbDeviceDto(busId, name, vidpid, null, state, attachedVm));
        }

        return devices;
    }
}