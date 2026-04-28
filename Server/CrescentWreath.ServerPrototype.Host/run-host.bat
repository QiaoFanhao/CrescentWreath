@echo off
setlocal

cd /d "%~dp0"

set "PORT=%~1"
if "%PORT%"=="" set "PORT=18080"

echo [CrescentWreath] Starting ServerPrototype.Host on port %PORT%...

dotnet run --project CrescentWreath.ServerPrototype.Host.csproj -- %PORT%
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo [CrescentWreath] Host exited with error code %EXIT_CODE%.
)

echo.
echo Press any key to close...
pause >nul
