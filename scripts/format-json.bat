@echo off
setlocal
if "%~1"=="" (
  echo Drag a JSON file onto this bat, or run:
  echo   format-json.bat input.json [output.json]
  exit /b 2
)
dotnet run -c Release --project "%~dp0JsonFastFormat.csproj" -- %*
