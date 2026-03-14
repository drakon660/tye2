@echo off
REM Starts VitePress dev server for Tye2 documentation
REM Requires: npm install (run once to install dependencies)

where npm >nul 2>&1
if %errorlevel% neq 0 (
    echo npm is not installed. Please install Node.js from https://nodejs.org
    exit /b 1
)

if not exist node_modules (
    if exist package-lock.json (
        echo Installing dependencies with npm ci...
        npm ci
    ) else (
        echo Installing dependencies...
        npm install
    )
)

npm run docs:dev
