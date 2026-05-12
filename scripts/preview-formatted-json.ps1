param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$JsonPath
)

$ErrorActionPreference = 'Stop'

$source = (Resolve-Path -LiteralPath $JsonPath).Path
$tool = Join-Path $PSScriptRoot 'bin\Release\net9.0\win-x64\publish\jsonfmt.exe'

if (-not (Test-Path -LiteralPath $tool)) {
    throw "Cannot find jsonfmt.exe: $tool"
}

$previewDir = Join-Path $env:TEMP 'JsonFastFormatPreview'
New-Item -ItemType Directory -Path $previewDir -Force | Out-Null

$baseName = [IO.Path]::GetFileNameWithoutExtension($source)
$safeName = ($baseName -replace '[^\w\-. ]', '_')
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$previewPath = Join-Path $previewDir "$safeName.formatted.$timestamp.json"

& $tool $source $previewPath
if ($LASTEXITCODE -ne 0) {
    throw "jsonfmt.exe failed with exit code $LASTEXITCODE"
}

Start-Process notepad.exe -ArgumentList @($previewPath)
