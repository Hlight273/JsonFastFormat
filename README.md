# JsonFastFormat

JsonFastFormat is a local streaming JSON formatter for Windows. It is designed for large JSON files that are slow to format in Notepad or VS Code.

## For Normal Users

Open:

```text
package\JsonFastFormatManager.exe
```

Use the GUI to enable or disable the JSON right-click menu. After enabling it, right-click a `.json` file and choose:

```text
通过记事本预览格式化后的json
```

The preview is written to `%TEMP%\JsonFastFormatPreview` and opened with Notepad. The original JSON file is not modified.

## Project Structure

```text
assets/                         icon and visual resources
src/JsonFastFormat.Cli/          streaming formatter CLI
src/JsonFastFormat.Manager/      WinForms GUI and right-click handler
scripts/                         legacy PowerShell/bat helpers and package script
package/                         user-facing packaged files
```

## Build

```powershell
dotnet build .\JsonFastFormat.sln -c Release
```

## Build Package

For one-click packaging, double-click:

```text
tools\package-exe.bat
```

Or run the underlying script directly:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-package.ps1
```

The package directory will contain:

```text
JsonFastFormatManager.exe
jsonfmt.exe
JsonFastFormat.ico
README.txt
```

The packaged executables are self-contained Windows x64 single-file apps.
