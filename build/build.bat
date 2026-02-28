@echo off
setlocal ENABLEDELAYEDEXPANSION

set ROOT=%~dp0..
set VERSION_JSON=%~dp0version.json
set DIST=%ROOT%\dist
set HOST_DIST=%DIST%\host
set GUEST_DIST=%DIST%\guest

echo Neue Version setzen? (y/n)
set /p SETVER=

set VERSION=
set COMMENT=

if /I "%SETVER%"=="y" (
  set /p VERSION=SemVer ^(z.B. 1.3.0^): 
) else (
  for /f "tokens=2 delims=:," %%a in ('findstr /i "\"version\"" "%VERSION_JSON%"') do (
    set VERSION=%%~a
    set VERSION=!VERSION: =!
    set VERSION=!VERSION:"=!
  )
)

if "%VERSION%"=="" set VERSION=0.1.0

set /p COMMENT=Build Kommentar/Changelog kurz: 

for /f %%i in ('git rev-parse --short HEAD 2^>nul') do set GITCOMMIT=%%i
if "%GITCOMMIT%"=="" set GITCOMMIT=unknown

for /f %%i in ('powershell -NoProfile -Command "(Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')"') do set NOW=%%i

(
  echo {
  echo   "version": "%VERSION%",
  echo   "comment": "%COMMENT%",
  echo   "timestamp": "%NOW%",
  echo   "gitCommit": "%GITCOMMIT%"
  echo }
) > "%VERSION_JSON%"

echo [1/5] dotnet restore
dotnet restore "%ROOT%\UsbPassthrough.sln" || goto :err

echo [2/5] dotnet test
dotnet msbuild "%ROOT%\tests\UsbPassthrough.Backend.Tests\UsbPassthrough.Backend.Tests.csproj" -t:Test -p:Configuration=Release -p:UseMSBuildTestInfrastructure=true || goto :err

echo [3/5] publish HostTray
dotnet publish "%ROOT%\src\UsbPassthrough.HostTray\UsbPassthrough.HostTray.csproj" -c Release -r win-x64 --self-contained false -o "%HOST_DIST%\tray" || goto :err

echo [4/5] publish HostService
dotnet publish "%ROOT%\src\UsbPassthrough.HostService\UsbPassthrough.HostService.csproj" -c Release -r win-x64 --self-contained false -o "%HOST_DIST%\service" || goto :err

echo [5/5] publish GuestAgent
dotnet publish "%ROOT%\src\UsbPassthrough.GuestAgent\UsbPassthrough.GuestAgent.csproj" -c Release -r win-x64 --self-contained false -o "%GUEST_DIST%\agent" || goto :err

echo [6/6] publish GuestClient
dotnet publish "%ROOT%\src\UsbPassthrough.Cli\UsbPassthrough.Cli.csproj" -c Release -r win-x64 --self-contained false -o "%GUEST_DIST%\client" || goto :err

if exist "%ROOT%\Assets\Icons" xcopy /E /I /Y "%ROOT%\Assets\Icons" "%HOST_DIST%\icons" >nul
if exist "%ROOT%\Assets\Icons" xcopy /E /I /Y "%ROOT%\Assets\Icons" "%GUEST_DIST%\icons" >nul

copy /Y "%ROOT%\build\version.json" "%HOST_DIST%\version.json" >nul
copy /Y "%ROOT%\build\version.json" "%GUEST_DIST%\version.json" >nul

copy /Y "%HOST_DIST%\service\usb-host-service.exe" "%HOST_DIST%\server.exe" >nul
copy /Y "%HOST_DIST%\tray\usb-host-tray.exe" "%HOST_DIST%\host-ui.exe" >nul
copy /Y "%GUEST_DIST%\agent\usb-guest-agent.exe" "%GUEST_DIST%\guest-agent.exe" >nul
copy /Y "%GUEST_DIST%\client\usb-guest-client.exe" "%GUEST_DIST%\client.exe" >nul

if exist "%ROOT%\src\UsbPassthrough.HostService\appsettings.template.json" copy /Y "%ROOT%\src\UsbPassthrough.HostService\appsettings.template.json" "%HOST_DIST%\config.template.json" >nul
if exist "%ROOT%\src\UsbPassthrough.GuestAgent\guest.template.json" copy /Y "%ROOT%\src\UsbPassthrough.GuestAgent\guest.template.json" "%GUEST_DIST%\guest.template.json" >nul

(
  echo @echo off
  echo setlocal
  echo sc query UsbPassthroughHost ^>nul 2^>^&1
  echo if %%errorlevel%% neq 0 sc create UsbPassthroughHost binPath^= "%%~dp0service\usb-host-service.exe" start^= auto
  echo sc start UsbPassthroughHost
  echo start "" "%%~dp0tray\usb-host-tray.exe"
  echo echo Host service und UI wurden gestartet.
) > "%HOST_DIST%\install-host-service.bat"

(
  echo @echo off
  echo setlocal
  echo sc query UsbPassthroughGuest ^>nul 2^>^&1
  echo if %%errorlevel%% neq 0 sc create UsbPassthroughGuest binPath^= "%%~dp0agent\usb-guest-agent.exe" start^= auto
  echo sc start UsbPassthroughGuest
  echo echo Guest Agent wurde gestartet.
) > "%GUEST_DIST%\install-guest-agent.bat"

echo Self-signed Zertifikate erzeugen? (y/n)
set /p MAKECERT=
if /I "%MAKECERT%"=="y" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "
    $certDir = Join-Path '%DIST%' 'certs';
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null;
    $pwd = ConvertTo-SecureString 'UsbPassthrough!2026' -AsPlainText -Force;
    $cert = New-SelfSignedCertificate -Subject 'CN=UsbPassthrough' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy Exportable -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(3);
    Export-Certificate -Cert $cert -FilePath (Join-Path $certDir 'UsbPassthrough.cer') | Out-Null;
    Export-PfxCertificate -Cert $cert -FilePath (Join-Path $certDir 'UsbPassthrough.pfx') -Password $pwd | Out-Null;
    Set-Content -Path (Join-Path $certDir 'README.txt') -Value 'PFX Passwort: UsbPassthrough!2026';
  " || goto :err
)

powershell -NoProfile -Command "Compress-Archive -Path '%HOST_DIST%\*' -DestinationPath '%DIST%\dist-host-%VERSION%.zip' -Force" >nul
powershell -NoProfile -Command "Compress-Archive -Path '%GUEST_DIST%\*' -DestinationPath '%DIST%\dist-guest-%VERSION%.zip' -Force" >nul

echo Fertig. Artefakte in dist\
exit /b 0

:err
echo Build fehlgeschlagen.
pause
exit /b 1