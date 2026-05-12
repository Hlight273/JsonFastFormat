$ErrorActionPreference = 'Stop'

$menuKey = 'Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.json\shell\JsonFastFormatPreview'

if (Test-Path -LiteralPath $menuKey) {
    Remove-Item -LiteralPath $menuKey -Recurse -Force
    Write-Host 'Removed context menu item.'
} else {
    Write-Host 'Context menu item is not installed.'
}
