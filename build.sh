#!/bin/bash

# EasySave V1.0 Build Script
# This script builds the EasySave project for multiple platforms

echo "================================"
echo "EasySave V1.0 Build Script"
echo "================================"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null
then
    echo "Error: .NET SDK is not installed"
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "Checking .NET version..."
dotnet --version
echo ""

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean EasySave.sln
echo ""

# Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore EasySave.sln
echo ""

# Build Debug configuration
echo "Building Debug configuration..."
dotnet build EasySave.sln -c Debug
if [ $? -eq 0 ]; then
    echo "✓ Debug build successful"
else
    echo "✗ Debug build failed"
    exit 1
fi
echo ""

# Build Release configuration
echo "Building Release configuration..."
dotnet build EasySave.sln -c Release
if [ $? -eq 0 ]; then
    echo "✓ Release build successful"
else
    echo "✗ Release build failed"
    exit 1
fi
echo ""

# Publish for Windows
echo "Publishing for Windows (x64)..."
dotnet publish EasySave/EasySave.csproj -c Release -r win-x64 --self-contained false -o ./publish/win-x64
if [ $? -eq 0 ]; then
    echo "✓ Windows x64 publish successful"
else
    echo "✗ Windows x64 publish failed"
fi
echo ""

# Publish for Linux
echo "Publishing for Linux (x64)..."
dotnet publish EasySave/EasySave.csproj -c Release -r linux-x64 --self-contained false -o ./publish/linux-x64
if [ $? -eq 0 ]; then
    echo "✓ Linux x64 publish successful"
else
    echo "✗ Linux x64 publish failed"
fi
echo ""

# Publish for macOS
echo "Publishing for macOS (x64)..."
dotnet publish EasySave/EasySave.csproj -c Release -r osx-x64 --self-contained false -o ./publish/osx-x64
if [ $? -eq 0 ]; then
    echo "✓ macOS x64 publish successful"
else
    echo "✗ macOS x64 publish failed"
fi
echo ""

echo "================================"
echo "Build completed successfully!"
echo "================================"
echo ""
echo "Published binaries available in ./publish/"
echo "  - Windows: ./publish/win-x64/"
echo "  - Linux:   ./publish/linux-x64/"
echo "  - macOS:   ./publish/osx-x64/"
echo ""
echo "Note: These builds require .NET 8.0 Runtime to be installed on target systems"
echo ""
