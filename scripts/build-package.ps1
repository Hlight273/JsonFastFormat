$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'package'
$cliProject = Join-Path $root 'src\JsonFastFormat.Cli\JsonFastFormat.Cli.csproj'
$managerProject = Join-Path $root 'src\JsonFastFormat.Manager\JsonFastFormat.Manager.csproj'

dotnet publish $cliProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $package
dotnet publish $managerProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $package

Copy-Item -LiteralPath (Join-Path $root 'assets\JsonFastFormat.ico') -Destination (Join-Path $package 'JsonFastFormat.ico') -Force
Get-ChildItem -LiteralPath $package -Filter '*.pdb' -File | Remove-Item -Force

Write-Host "Package ready: $package"
