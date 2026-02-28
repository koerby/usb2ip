# INSTALL_HOST

1. `build\\build.bat` ausführen, damit `dist\\host` erzeugt wird.
2. `usb-host-service.exe` als Windows-Service registrieren (`sc create`).
3. Optional `usb-host-tray.exe` via Autostart starten.
4. Firewall auf Host-only/Subnet für TCP 3240 begrenzen.

Alternative: `dist\\host\\install-host-service.bat` verwenden.

## Testsigning (falls nötig)

- `bcdedit /set testsigning on`
- Neustart durchführen.