@echo off
setlocal EnableExtensions

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
call :get_gitversion_var AssemblySemVer ASSEMBLY_SEMVER || goto :fail
call :get_gitversion_var AssemblySemFileVer ASSEMBLY_FILEVER || goto :fail
call :get_gitversion_var SemVer NUGET_VERSION || goto :fail
call :get_gitversion_var InformationalVersion INFO_VERSION || goto :fail

echo Version: %NUGET_VERSION%
echo Publishing tye2 in Release mode...
dotnet publish "%PROJECT%" -c Release -o "%OUTPUT%" --nologo ^
  -p:AssemblyVersion=%ASSEMBLY_SEMVER% ^
  -p:FileVersion=%ASSEMBLY_FILEVER% ^
  -p:InformationalVersion=%INFO_VERSION% ^
  -p:Version=%NUGET_VERSION% || goto :fail

popd
echo Build completed. Output: %OUTPUT%
exit /b 0

:get_gitversion_var
set "GV_NAME=%~1"
set "GV_TARGET=%~2"
set "GV_TMP=%TEMP%\gitversion_%RANDOM%%RANDOM%.txt"

dotnet-gitversion /showvariable %GV_NAME% > "%GV_TMP%" 2>&1
if errorlevel 1 (
  echo ERROR: GitVersion failed while resolving %GV_NAME%.
  type "%GV_TMP%"
  del /q "%GV_TMP%" >nul 2>nul
  exit /b 1
)

set /p GV_VALUE=<"%GV_TMP%"
del /q "%GV_TMP%" >nul 2>nul

if "%GV_VALUE%"=="" (
  echo ERROR: GitVersion returned an empty value for %GV_NAME%.
  exit /b 1
)

set "%GV_TARGET%=%GV_VALUE%"
exit /b 0

:fail
set "CODE=%ERRORLEVEL%"
popd
exit /b %CODE%
