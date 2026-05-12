@echo off
setlocal
set "NO_PAUSE="
if /I "%~1"=="--no-pause" set "NO_PAUSE=1"

set "ROOT=%~dp0.."
set "SCRIPT=%ROOT%\scripts\build-package.ps1"

if not exist "%SCRIPT%" (
  echo Cannot find build script:
  echo   "%SCRIPT%"
  if not defined NO_PAUSE pause
  exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
  echo.
  echo Package failed. Exit code: %EXIT_CODE%
  if not defined NO_PAUSE pause
  exit /b %EXIT_CODE%
)

echo.
echo Package complete:
echo   "%ROOT%\package"
if not defined NO_PAUSE pause
