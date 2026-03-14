#!/bin/bash
# Starts VitePress dev server for Tye2 documentation
# Requires: npm install (run once to install dependencies)

if ! command -v npm &> /dev/null; then
    echo "npm is not installed. Please install Node.js from https://nodejs.org"
    exit 1
fi

if [ ! -d "node_modules" ]; then
    if [ -f "package-lock.json" ]; then
        echo "Installing dependencies with npm ci..."
        npm ci
    else
        echo "Installing dependencies..."
        npm install
    fi
fi

npm run docs:dev
