@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "PROJECT=%REPO_ROOT%\src\tye2\tye2.csproj"
set "OUTPUT=%REPO_ROOT%\artifacts\release\tye2"

if not exist "%PROJECT%" (
  echo ERROR: Project file not found: %PROJECT%
  exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet CLI not found in PATH.
  exit /b 1
)

pushd "%REPO_ROOT%"

echo Restoring solution...
dotnet restore "tye2.sln" || goto :fail

echo Publishing tye2 in Release mode...
dotnet publish "%PROJECT%" -c Release -o "%OUTPUT%" --nologo || goto :fail

popd
echo Build completed. Output: %OUTPUT%
exit /b 0

:fail
set "CODE=%ERRORLEVEL%"
popd
exit /b %CODE%
