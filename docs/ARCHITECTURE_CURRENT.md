# Architecture Current State

## Bestand (vor Umbau)

- Primärprojekt ist `Usbipd` als kombinierte CLI + Windows-Service (`usbipd`).
- Der Host-Teil läuft als Service (`ICommandHandlers.Server` + `Server`), akzeptiert USB/IP-Verbindungen via TCP (Default Port 3240).
- Attach/Detach/Bind/Unbind wird im CLI-Layer (`CommandHandlersCli`) orchestriert.
- Geräte- und Policy-Zustand wird über Registry/Driver-Utilities verwaltet (`UsbipdRegistry`, `DriverTools`, `Policy`, `WindowsDevice`).
- Guest-seitige Anbindung erfolgt heute über externe USB/IP-Clients (Linux/WSL), nicht über einen dedizierten Windows Guest-Service in diesem Repo.
- Treiber-Installation/-Wartung liegt in Installer-Kommandos (`CommandHandlersInstaller`) und WiX-Projekt unter `Installer`.

## Host vs Guest (Ist)

- Host (Windows mit physischem USB): `usbipd` Service + CLI, Treiberpakete in `Drivers`.
- Guest: überwiegend Linux/WSL Tooling (`usbip`/`usbipd attach --wsl`), kein separater, persistenter GuestAgent als eigenes Projekt.

## Build/Release (Ist)

- Bestehende Solution: `usb2ip.sln` mit `Usbipd`, `Usbipd.PowerShell`, `UnitTests`.
- Installer-Build über WiX-Projekt in `Installer`.

## Zielableitung für Umbau

- Backend-Kopplung an Bestandsfunktion via `usbipd`-CLI/API kapseln.
- HostService/HostTray strikt über IPC (Named Pipes) trennen.
- GuestAgent als separater Windows-Service ergänzen.