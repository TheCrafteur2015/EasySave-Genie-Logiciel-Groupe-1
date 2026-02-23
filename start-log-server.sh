#!/bin/bash

echo "=========================================="
echo "  EasySave Log Server - Quick Start"
echo "=========================================="
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

echo "✅ Docker found"

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo "✅ Docker Compose found"
echo ""

# Build and start the server
echo "🔨 Building Docker image..."
docker-compose build

echo ""
echo "🚀 Starting Log Server..."
docker-compose up -d

echo ""
echo "⏳ Waiting for server to be ready..."
sleep 5

# Health check
if curl -s http://localhost:5000/api/logs/health > /dev/null; then
    echo "✅ Server is running successfully!"
    echo ""
    echo "📍 Server URL: http://localhost:5000"
    echo "📂 Logs directory: ./logs/"
    echo ""
    echo "🔍 View logs: docker logs easysave-logserver -f"
    echo "🛑 Stop server: docker-compose down"
else
    echo "⚠️ Server might not be ready yet. Check logs:"
    echo "   docker logs easysave-logserver"
fi

echo ""
echo "=========================================="
