# USB Passthrough (Host/Client)

Dieses Repository ist auf eine schlanke Windows-Architektur reduziert:

- Host/Server: teilt lokale USB-Geräte.
- Guest/Client: verbindet sich über IP auf freigegebene Geräte.

## Struktur

- `src/UsbPassthrough.HostService` – Windows-Dienst (Server/Share-Logik)
- `src/UsbPassthrough.HostTray` – WPF Light-Mode UI + Tray
- `src/UsbPassthrough.GuestAgent` – Windows-Dienst im Client/Gast
- `src/UsbPassthrough.Cli` – `usb-guest-client.exe` (connect/disconnect/list)
- `src/UsbPassthrough.Backend` – kapselt Aufrufe gegen `usbipd`
- `src/UsbPassthrough.Common` – IPC Contracts + gemeinsame Modelle
- `build/build.bat` – kompletter Build inkl. Dist + optional Self-Signed Zertifikate

## Build

Unter Windows:

```bat
build\build.bat
```

Danach liegen fertige Artefakte in `dist/host` und `dist/guest`.

## Start

- Host: `dist\host\install-host-service.bat`
- Guest: `dist\guest\install-guest-agent.bat`
- Client-Connect: `dist\guest\client.exe connect --remote <HOST-IP> --busid <BUSID>`

## Doku

- `docs/INSTALL_HOST.md`
- `docs/INSTALL_GUEST.md`
- `docs/TROUBLESHOOTING.md`
