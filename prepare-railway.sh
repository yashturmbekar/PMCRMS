#!/bin/bash

# Railway Deployment Quick Start Script
# Run this to prepare your project for Railway deployment

echo "🚀 Preparing PMCRMS for Railway Deployment..."
echo ""

# Check if we're in the right directory
if [ ! -f "Ezybricks.sln" ]; then
    echo "❌ Error: Please run this script from the repository root (where Ezybricks.sln is located)"
    exit 1
fi

echo "✅ Repository structure verified"
echo ""

# Check if railway.toml exists
if [ ! -f "railway.toml" ]; then
    echo "❌ Error: railway.toml not found in repository root"
    exit 1
fi

echo "✅ railway.toml found"
echo ""

# Check if Dockerfile exists
if [ ! -f "PMCRMS/backend/PMCRMS.API/Dockerfile" ]; then
    echo "❌ Error: Dockerfile not found at PMCRMS/backend/PMCRMS.API/Dockerfile"
    exit 1
fi

echo "✅ Dockerfile found"
echo ""

# Check if appsettings.Production.json exists
if [ ! -f "PMCRMS/backend/PMCRMS.API/appsettings.Production.json" ]; then
    echo "❌ Error: appsettings.Production.json not found"
    exit 1
fi

echo "✅ Production settings found"
echo ""

# Git status
echo "📋 Checking git status..."
git status --short

echo ""
echo "📝 Next steps:"
echo "1. Commit and push your changes:"
echo "   git add ."
echo "   git commit -m 'Add Railway deployment configuration'"
echo "   git push origin main"
echo ""
echo "2. Go to https://railway.app and create a new project"
echo "3. Connect your GitHub repository"
echo "4. Add PostgreSQL database"
echo "5. Configure environment variables (see RAILWAY_DEPLOYMENT.md)"
echo ""
echo "✨ Ready for deployment!"
