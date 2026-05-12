$ErrorActionPreference = 'Stop'

function U([int[]]$Codes) {
    return -join ($Codes | ForEach-Object { [char]$_ })
}

$menuText = U @(0x901A,0x8FC7,0x8BB0,0x4E8B,0x672C,0x9884,0x89C8,0x683C,0x5F0F,0x5316,0x540E,0x7684,0x006A,0x0073,0x006F,0x006E)
$iconPath = Join-Path $PSScriptRoot 'JsonFastFormat.ico'
$scriptPath = Join-Path $PSScriptRoot 'preview-formatted-json.ps1'
$toolPath = Join-Path $PSScriptRoot 'bin\Release\net9.0\win-x64\publish\jsonfmt.exe'
$menuKey = 'Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.json\shell\JsonFastFormatPreview'
$commandKey = Join-Path $menuKey 'command'

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Cannot find preview script: $scriptPath"
}

if (-not (Test-Path -LiteralPath $toolPath)) {
    throw "Cannot find jsonfmt.exe: $toolPath. Run dotnet publish first."
}

New-Item -Path $commandKey -Force | Out-Null
New-ItemProperty -Path $menuKey -Name 'MUIVerb' -Value $menuText -PropertyType String -Force | Out-Null
if (Test-Path -LiteralPath $iconPath) {
    New-ItemProperty -Path $menuKey -Name 'Icon' -Value $iconPath -PropertyType String -Force | Out-Null
} else {
    New-ItemProperty -Path $menuKey -Name 'Icon' -Value $toolPath -PropertyType String -Force | Out-Null
}
New-ItemProperty -Path $commandKey -Name '(default)' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" `"%1`"" -PropertyType String -Force | Out-Null

Write-Host "Installed context menu item: $menuText"
Write-Host "Registry key: $menuKey"
