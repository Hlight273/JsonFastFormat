$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function U([int[]]$Codes) {
    return -join ($Codes | ForEach-Object { [char]$_ })
}

$menuKey = 'Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.json\shell\JsonFastFormatPreview'
$commandKey = Join-Path $menuKey 'command'
$scriptPath = Join-Path $PSScriptRoot 'preview-formatted-json.ps1'
$toolPath = Join-Path $PSScriptRoot 'bin\Release\net9.0\win-x64\publish\jsonfmt.exe'
$iconPath = Join-Path $PSScriptRoot 'JsonFastFormat.ico'
$menuText = U @(0x901A,0x8FC7,0x8BB0,0x4E8B,0x672C,0x9884,0x89C8,0x683C,0x5F0F,0x5316,0x540E,0x7684,0x006A,0x0073,0x006F,0x006E)

function Test-Installed {
    return Test-Path -LiteralPath $menuKey
}

function Install-Menu {
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "Cannot find preview script: $scriptPath"
    }

    if (-not (Test-Path -LiteralPath $toolPath)) {
        throw "Cannot find jsonfmt.exe: $toolPath"
    }

    New-Item -Path $commandKey -Force | Out-Null
    New-ItemProperty -Path $menuKey -Name 'MUIVerb' -Value $menuText -PropertyType String -Force | Out-Null
    if (Test-Path -LiteralPath $iconPath) {
        New-ItemProperty -Path $menuKey -Name 'Icon' -Value $iconPath -PropertyType String -Force | Out-Null
    } else {
        New-ItemProperty -Path $menuKey -Name 'Icon' -Value $toolPath -PropertyType String -Force | Out-Null
    }
    New-ItemProperty -Path $commandKey -Name '(default)' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" `"%1`"" -PropertyType String -Force | Out-Null
}

function Uninstall-Menu {
    if (Test-Path -LiteralPath $menuKey) {
        Remove-Item -LiteralPath $menuKey -Recurse -Force
    }
}

[System.Windows.Forms.Application]::EnableVisualStyles()

$form = New-Object System.Windows.Forms.Form
$form.Text = U @(0x004A,0x0073,0x006F,0x006E,0x0046,0x0061,0x0073,0x0074,0x0046,0x006F,0x0072,0x006D,0x0061,0x0074,0x0020,0x53F3,0x952E,0x83DC,0x5355)
$form.StartPosition = 'CenterScreen'
$form.ClientSize = New-Object System.Drawing.Size(430, 210)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
if (Test-Path -LiteralPath $iconPath) {
    $form.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($iconPath)
}

$title = New-Object System.Windows.Forms.Label
$title.Text = U @(0x004A,0x0053,0x004F,0x004E,0x0020,0x53F3,0x952E,0x83DC,0x5355,0x5F00,0x5173)
$title.Font = New-Object System.Drawing.Font('Microsoft YaHei UI', 14, [System.Drawing.FontStyle]::Bold)
$title.Location = New-Object System.Drawing.Point(22, 18)
$title.Size = New-Object System.Drawing.Size(380, 30)

$status = New-Object System.Windows.Forms.Label
$status.Font = New-Object System.Drawing.Font('Microsoft YaHei UI', 10)
$status.Location = New-Object System.Drawing.Point(24, 60)
$status.Size = New-Object System.Drawing.Size(380, 28)

$detail = New-Object System.Windows.Forms.Label
$detail.Text = (U @(0x83DC,0x5355,0x9879,0x003A,0x0020)) + $menuText
$detail.Font = New-Object System.Drawing.Font('Microsoft YaHei UI', 9)
$detail.Location = New-Object System.Drawing.Point(24, 92)
$detail.Size = New-Object System.Drawing.Size(380, 24)

$installButton = New-Object System.Windows.Forms.Button
$installButton.Text = U @(0x542F,0x7528,0x53F3,0x952E,0x83DC,0x5355)
$installButton.Location = New-Object System.Drawing.Point(27, 132)
$installButton.Size = New-Object System.Drawing.Size(120, 34)

$uninstallButton = New-Object System.Windows.Forms.Button
$uninstallButton.Text = U @(0x5173,0x95ED,0x53F3,0x952E,0x83DC,0x5355)
$uninstallButton.Location = New-Object System.Drawing.Point(157, 132)
$uninstallButton.Size = New-Object System.Drawing.Size(120, 34)

$closeButton = New-Object System.Windows.Forms.Button
$closeButton.Text = U @(0x5173,0x95ED)
$closeButton.Location = New-Object System.Drawing.Point(287, 132)
$closeButton.Size = New-Object System.Drawing.Size(90, 34)

function Refresh-Status {
    if (Test-Installed) {
        $status.Text = U @(0x5F53,0x524D,0x72B6,0x6001,0x003A,0x0020,0x5DF2,0x542F,0x7528)
        $status.ForeColor = [System.Drawing.Color]::FromArgb(20, 120, 60)
        $installButton.Enabled = $false
        $uninstallButton.Enabled = $true
    } else {
        $status.Text = U @(0x5F53,0x524D,0x72B6,0x6001,0x003A,0x0020,0x672A,0x542F,0x7528)
        $status.ForeColor = [System.Drawing.Color]::FromArgb(160, 70, 30)
        $installButton.Enabled = $true
        $uninstallButton.Enabled = $false
    }
}

$installButton.Add_Click({
    try {
        Install-Menu
        Refresh-Status
        [System.Windows.Forms.MessageBox]::Show((U @(0x53F3,0x952E,0x83DC,0x5355,0x5DF2,0x542F,0x7528,0x3002)), 'JsonFastFormat', 'OK', 'Information') | Out-Null
    } catch {
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, (U @(0x542F,0x7528,0x5931,0x8D25)), 'OK', 'Error') | Out-Null
    }
})

$uninstallButton.Add_Click({
    try {
        Uninstall-Menu
        Refresh-Status
        [System.Windows.Forms.MessageBox]::Show((U @(0x53F3,0x952E,0x83DC,0x5355,0x5DF2,0x5173,0x95ED,0x3002)), 'JsonFastFormat', 'OK', 'Information') | Out-Null
    } catch {
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, (U @(0x5173,0x95ED,0x5931,0x8D25)), 'OK', 'Error') | Out-Null
    }
})

$closeButton.Add_Click({ $form.Close() })

$form.Controls.AddRange(@($title, $status, $detail, $installButton, $uninstallButton, $closeButton))
Refresh-Status
[void]$form.ShowDialog()
