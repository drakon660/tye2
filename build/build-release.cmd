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

where dotnet-gitversion >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet-gitversion not found in PATH.
  echo Install with: dotnet tool install --global GitVersion.Tool
  exit /b 1
)

pushd "%REPO_ROOT%"

echo Restoring solution...
dotnet restore "tye2.sln" || goto :fail

echo Calculating version with GitVersion...
for /f "delims=" %%i in ('dotnet-gitversion /showvariable AssemblySemVer') do set "ASSEMBLY_SEMVER=%%i"
for /f "delims=" %%i in ('dotnet-gitversion /showvariable AssemblySemFileVer') do set "ASSEMBLY_FILEVER=%%i"
for /f "delims=" %%i in ('dotnet-gitversion /showvariable NuGetVersionV2') do set "NUGET_VERSION=%%i"

if "%ASSEMBLY_SEMVER%"=="" (
  echo ERROR: Failed to resolve AssemblySemVer from GitVersion.
  goto :fail
)
if "%ASSEMBLY_FILEVER%"=="" (
  echo ERROR: Failed to resolve AssemblySemFileVer from GitVersion.
  goto :fail
)
if "%NUGET_VERSION%"=="" (
  echo ERROR: Failed to resolve NuGetVersionV2 from GitVersion.
  goto :fail
)

echo Version: %NUGET_VERSION%
echo Publishing tye2 in Release mode...
dotnet publish "%PROJECT%" -c Release -o "%OUTPUT%" --nologo ^
  -p:AssemblyVersion=%ASSEMBLY_SEMVER% ^
  -p:FileVersion=%ASSEMBLY_FILEVER% ^
  -p:InformationalVersion=%NUGET_VERSION% ^
  -p:Version=%NUGET_VERSION% || goto :fail

popd
echo Build completed. Output: %OUTPUT%
exit /b 0

:fail
set "CODE=%ERRORLEVEL%"
popd
exit /b %CODE%
