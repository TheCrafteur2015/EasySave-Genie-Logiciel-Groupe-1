@echo off
REM EasySave V1.0 Build Script for Windows
REM This script builds the EasySave project for multiple platforms

echo ================================
echo EasySave V1.0 Build Script
echo ================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: .NET SDK is not installed
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Checking .NET version...
dotnet --version
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean EasySave.sln
echo.

REM Restore dependencies
echo Restoring NuGet packages...
dotnet restore EasySave.sln
echo.

REM Build Debug configuration
echo Building Debug configuration...
dotnet build EasySave.sln -c Debug
if %ERRORLEVEL% EQU 0 (
    echo [32m✓ Debug build successful[0m
) else (
    echo [31m✗ Debug build failed[0m
    pause
    exit /b 1
)
echo.

REM Build Release configuration
echo Building Release configuration...
dotnet build EasySave.sln -c Release
if %ERRORLEVEL% EQU 0 (
    echo [32m✓ Release build successful[0m
) else (
    echo [31m✗ Release build failed[0m
    pause
    exit /b 1
)
echo.

REM Publish for Windows
echo Publishing for Windows (x64)...
dotnet publish EasySave/EasySave.csproj -c Release -r win-x64 --self-contained false -o ./publish/win-x64
if %ERRORLEVEL% EQU 0 (
    echo [32m✓ Windows x64 publish successful[0m
) else (
    echo [31m✗ Windows x64 publish failed[0m
)
echo.

REM Publish for Linux
echo Publishing for Linux (x64)...
dotnet publish EasySave/EasySave.csproj -c Release -r linux-x64 --self-contained false -o ./publish/linux-x64
if %ERRORLEVEL% EQU 0 (
    echo [32m✓ Linux x64 publish successful[0m
) else (
    echo [31m✗ Linux x64 publish failed[0m
)
echo.

REM Publish for macOS
echo Publishing for macOS (x64)...
dotnet publish EasySave/EasySave.csproj -c Release -r osx-x64 --self-contained false -o ./publish/osx-x64
if %ERRORLEVEL% EQU 0 (
    echo [32m✓ macOS x64 publish successful[0m
) else (
    echo [31m✗ macOS x64 publish failed[0m
)
echo.

echo ================================
echo Build completed successfully!
echo ================================
echo.
echo Published binaries available in .\publish\
echo   - Windows: .\publish\win-x64\
echo   - Linux:   .\publish\linux-x64\
echo   - macOS:   .\publish\osx-x64\
echo.
echo Note: These builds require .NET 8.0 Runtime to be installed on target systems
echo.
pause
