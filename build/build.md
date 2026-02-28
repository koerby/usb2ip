# Build

## Windows lokal

1. `build\\build.bat` starten.
2. Interaktive Fragen beantworten (Version, Kommentar, optional Zertifikate).
3. Artefakte liegen unter `dist\\host` und `dist\\guest`.
4. Host: `install-host-service.bat` ausführen (Service + UI Autostart).
5. Guest: `install-guest-agent.bat` ausführen und `client.exe` für Connect/Disconnect verwenden.

## Codespaces / Linux

`build.bat` ist für `cmd.exe` gedacht. In Linux direkt:

```bash
dotnet restore UsbPassthrough.sln
dotnet test tests/UsbPassthrough.Backend.Tests/UsbPassthrough.Backend.Tests.csproj -c Release
```

Hinweis: WPF-Publishing ist unter Linux nicht ausführbar; finalen Publish auf Windows ausführen.

## Dist Inhalt

- Host: `server.exe`, `host-ui.exe`, `service\\usb-host-service.exe`, `tray\\usb-host-tray.exe`, `install-host-service.bat`.
- Guest: `guest-agent.exe`, `client.exe`, `agent\\usb-guest-agent.exe`, `client\\usb-guest-client.exe`, `install-guest-agent.bat`.
- Optional Zertifikate: `dist\\certs\\UsbPassthrough.cer` und `UsbPassthrough.pfx`.