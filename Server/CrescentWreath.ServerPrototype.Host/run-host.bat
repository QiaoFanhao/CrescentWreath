@echo off
setlocal

cd /d "%~dp0"

set "PORT=%~1"
if "%PORT%"=="" set "PORT=18080"
if "%CRESCENT_WS_BIND_HOST%"=="" set "CRESCENT_WS_BIND_HOST=+"

echo [CrescentWreath] Starting ServerPrototype.Host on %CRESCENT_WS_BIND_HOST%:%PORT%...
echo [CrescentWreath] LAN/Radmin clients should use the ws://IP:%PORT%/ws?mode=lobby address printed by the host.

dotnet run --project CrescentWreath.ServerPrototype.Host.csproj -- %PORT%
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo [CrescentWreath] Host exited with error code %EXIT_CODE%.
)

echo.
echo Press any key to close...
pause >nul
