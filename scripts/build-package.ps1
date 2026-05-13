$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'package'
$cliProject = Join-Path $root 'src\JsonFastFormat.Cli\JsonFastFormat.Cli.csproj'
$managerProject = Join-Path $root 'src\JsonFastFormat.Manager\JsonFastFormat.Manager.csproj'
$cliDir = Join-Path $root 'src\JsonFastFormat.Cli'
$managerDir = Join-Path $root 'src\JsonFastFormat.Manager'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FileName @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FileName failed with exit code $LASTEXITCODE"
    }
}

Remove-Item -LiteralPath (Join-Path $cliDir 'bin') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $cliDir 'obj') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managerDir 'bin') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managerDir 'obj') -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -LiteralPath $package -File | Where-Object { $_.Name -ne 'README.txt' } | Remove-Item -Force

Invoke-Checked dotnet @('publish', $cliProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:EnableCompressionInSingleFile=true', '-o', $package)
Invoke-Checked dotnet @('publish', $managerProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:EnableCompressionInSingleFile=true', '-o', $package)

Copy-Item -LiteralPath (Join-Path $root 'assets\JsonFastFormat.ico') -Destination (Join-Path $package 'JsonFastFormat.ico') -Force
Get-ChildItem -LiteralPath $package -Filter '*.pdb' -File | Remove-Item -Force

Write-Host "Package ready: $package"
