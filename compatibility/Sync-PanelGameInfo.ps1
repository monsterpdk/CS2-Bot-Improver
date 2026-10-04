param(
    [Parameter(Mandatory)][string]$Csgo,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'GameInfoCompatibility.psm1') -Force
$gamePath = (Resolve-Path -LiteralPath $Csgo).Path.TrimEnd('\')
if ([IO.Path]::GetFileName($gamePath) -ne 'csgo') { throw 'Select the game\csgo directory.' }
$currentPath = Join-Path $gamePath 'gameinfo.gi'
$raw = [IO.File]::ReadAllBytes($currentPath)
$hasBom = $raw.Length -ge 3 -and $raw[0] -eq 239 -and $raw[1] -eq 187 -and $raw[2] -eq 191
$encoding = New-Object System.Text.UTF8Encoding($hasBom, $true)
$text = $encoding.GetString($raw).TrimStart([char]0xFEFF)
$settingsPath = Join-Path $PSScriptRoot 'CompatibilitySettings.json'
$botProfiles = $false
if (Test-Path -LiteralPath $settingsPath) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($settings.BotProfiles -isnot [bool]) { throw 'BotProfiles must be a boolean in CompatibilitySettings.json.' }
    $botProfiles = $settings.BotProfiles
}
if ($botProfiles -and !(Test-Path -LiteralPath (Join-Path $gamePath 'overrides\botprofile.vpk') -PathType Leaf)) { throw 'Selected botprofile.vpk is missing.' }
$templates = New-PanelGameInfoTemplates -Text $text -BotProfiles:$botProfiles
foreach ($layer in $templates.Layers) {
    if ($layer -match '[/\\:]|^\.{1,2}$') { throw 'Unsupported LayeredOnMod path.' }
    if (!(Test-Path -LiteralPath (Join-Path (Split-Path $gamePath -Parent) ($layer + '\gameinfo.gi')))) {
        throw ('The current gameinfo references missing layer ' + $layer + '. Validate CS2 files in Steam before opening the panel.')
    }
}
if ($ValidateOnly) {
    Write-Output 'Current gameinfo is valid; mode templates can be generated without obsolete backups.'
    return
}
if (Get-Process cs2 -ErrorAction SilentlyContinue) { throw 'Close CS2 before synchronizing panel templates.' }
if (Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'Panel v*' }) { throw 'Close the old panel before synchronizing its templates.' }
$currentHash = (Get-FileHash -LiteralPath $currentPath).Hash
$backupRoot = Join-Path $gamePath '_botimprover_diagnostics\panel-gameinfo'
$backup = Join-Path $backupRoot ([DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath $currentPath -Destination (Join-Path $backup 'current-gameinfo.gi')
$targets = @(
    [PSCustomObject]@{ Mode = 'Online'; Text = $templates.Online },
    [PSCustomObject]@{ Mode = 'WithBots'; Text = $templates.Bots }
)
foreach ($target in $targets) {
    $path = Join-Path $gamePath ('backup\' + $target.Mode + '\gameinfo.gi')
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $backup ($target.Mode + '-gameinfo.gi')) }
}
if ((Get-FileHash -LiteralPath $currentPath).Hash -ne $currentHash) { throw 'Current gameinfo changed during preparation; nothing replaced.' }
try {
    foreach ($target in $targets) {
        $path = Join-Path $gamePath ('backup\' + $target.Mode + '\gameinfo.gi')
        New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
        [IO.File]::WriteAllText($path, $target.Text, $encoding)
        if ([IO.File]::ReadAllText($path) -ne $target.Text) { throw 'Template verification failed.' }
    }
} catch {
    foreach ($target in $targets) {
        $saved = Join-Path $backup ($target.Mode + '-gameinfo.gi')
        if (Test-Path -LiteralPath $saved) { Copy-Item -LiteralPath $saved -Destination (Join-Path $gamePath ('backup\' + $target.Mode + '\gameinfo.gi')) }
    }
    throw
}
if ((Get-FileHash -LiteralPath $currentPath).Hash -ne $currentHash) { throw 'Current gameinfo changed unexpectedly.' }
Write-Output ('Panel templates synchronized from current gameinfo. Backup: ' + $backup)
