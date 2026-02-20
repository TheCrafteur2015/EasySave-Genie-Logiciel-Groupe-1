@echo off
echo ==========================================
echo   EasySave Log Server - Quick Start
echo ==========================================
echo.

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

echo ✅ Docker is running
echo.

echo 🔨 Building Docker image...
docker-compose build

echo.
echo 🚀 Starting Log Server...
docker-compose up -d

echo.
echo ⏳ Waiting for server to be ready...
timeout /t 5 /nobreak >nul

REM Health check
curl -s http://localhost:5000/api/logs/health >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ Server is running successfully!
    echo.
    echo 📍 Server URL: http://localhost:5000
    echo 📂 Logs directory: .\logs\
    echo.
    echo 🔍 View logs: docker logs easysave-logserver -f
    echo 🛑 Stop server: docker-compose down
) else (
    echo ⚠️ Server might not be ready yet. Check logs:
    echo    docker logs easysave-logserver
)

echo.
echo ==========================================
pause
