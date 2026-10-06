param([string]$Csgo, [switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$payload = Join-Path $PSScriptRoot 'payload'
function Assert-RelativePath([string]$Path) {
    if (!$Path -or [IO.Path]::IsPathRooted($Path) -or $Path -match '(^|[/\\])\.\.([/\\]|$)|:') { throw 'Unsafe package path.' }
}
foreach ($entry in @($manifest.Files) + @($manifest.Dependencies)) { Assert-RelativePath $entry.Path }
foreach ($relative in $manifest.ParkPaths) { Assert-RelativePath $relative }
if (@($manifest.Files.Path | Select-Object -Unique).Count -ne $manifest.Files.Count) { throw 'Duplicate payload destination.' }
foreach ($entry in $manifest.Files) {
    $source = Join-Path $payload $entry.Path
    if (!(Test-Path -LiteralPath $source -PathType Leaf) -or (Get-FileHash -LiteralPath $source).Hash -ne $entry.SHA256) { throw ('Package damaged: ' + $entry.Path) }
}
if ($VerifyOnly) { Write-Output ('Package verified: ' + $manifest.Files.Count + ' files.'); return }
if (!$Csgo) { $Csgo = Read-Host 'CS2 game\csgo folder (full path)' }
$Csgo = (Resolve-Path -LiteralPath $Csgo.Trim('"')).Path.TrimEnd('\')
if ([IO.Path]::GetFileName($Csgo) -ne 'csgo' -or !(Test-Path -LiteralPath (Join-Path $Csgo 'gameinfo.gi'))) { throw 'Select the CS2 game\csgo folder.' }
if (Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq 'cs2' -or $_.ProcessName -like 'Panel v*' }) { throw 'Close CS2 and the panel first.' }
if ((Get-Content -LiteralPath (Join-Path $Csgo 'steam.inf') -Raw) -notmatch ('(?m)^ClientVersion=' + [regex]::Escape($manifest.ClientVersion) + '\s*$')) { throw ('Target version differs. Required CS2 ClientVersion ' + $manifest.ClientVersion) }
foreach ($entry in $manifest.Dependencies) {
    $target = Join-Path $Csgo $entry.Path
    if (!(Test-Path -LiteralPath $target -PathType Leaf) -or (Get-FileHash -LiteralPath $target).Hash -ne $entry.SHA256) { throw ('Dependency missing or different: ' + $entry.Path + '. Install the original v1.4.5 Windows pack first; use its bundled CounterStrikeSharp 376.') }
}
$pluginRoot = Join-Path $Csgo 'addons\counterstrikesharp\plugins'
if (Test-Path -LiteralPath $pluginRoot) {
    foreach ($directory in Get-ChildItem -LiteralPath $pluginRoot -Directory) {
        $relative = 'addons\counterstrikesharp\plugins\' + $directory.Name
        if ($directory.Name -notin $manifest.Plugins -and $relative -notin $manifest.ParkPaths) { throw ('Additional plugin found: ' + $directory.Name + '. Use a clean bot-pack installation.') }
    }
}
Import-Module (Join-Path $payload 'GameInfoCompatibility.psm1') -Force
$gameinfoPath = Join-Path $Csgo 'gameinfo.gi'
$raw = [IO.File]::ReadAllBytes($gameinfoPath)
$bom = $raw.Length -ge 3 -and $raw[0] -eq 239 -and $raw[1] -eq 187 -and $raw[2] -eq 191
$encoding = New-Object Text.UTF8Encoding($bom, $true)
$templates = New-PanelGameInfoTemplates -Text $encoding.GetString($raw).TrimStart([char]0xFEFF) -BotProfiles
foreach ($layer in $templates.Layers) {
    if ($layer -match '[/\\:]|^\.{1,2}$' -or !(Test-Path -LiteralPath (Join-Path (Split-Path $Csgo -Parent) ($layer + '\gameinfo.gi')))) { throw 'Current gameinfo references a missing layer; validate game files in Steam first.' }
}
$merged = @{}
if ($manifest.MergeCoreSettings) {
    $corePath = 'addons\counterstrikesharp\configs\core.json'
    $core = Get-Content -LiteralPath (Join-Path $Csgo $corePath) -Raw -Encoding UTF8 | ConvertFrom-Json
    $core | Add-Member -NotePropertyName AutoUpdateEnabled -NotePropertyValue $false -Force
    $core | Add-Member -NotePropertyName FollowCS2ServerGuidelines -NotePropertyValue $false -Force
    $merged[$corePath] = $core | ConvertTo-Json -Depth 20
}
$binaryMerged = @{}
if ($manifest.GenerateReactionProfiles) {
    Import-Module (Join-Path $payload 'ProfileCompatibility.psm1') -Force
    $binaryMerged = Get-ProfileUpdates -Csgo $Csgo -SettingsPath (Join-Path $payload 'ReactionSettings.json')
}
if ($manifest.MergePopulationSettings) {
    $path = 'ServerConfig.vdf'
    $text = if (Test-Path -LiteralPath (Join-Path $Csgo $path)) { [IO.File]::ReadAllText((Join-Path $Csgo $path)) } else { [IO.File]::ReadAllText((Join-Path $payload 'DefaultServerConfig.vdf')) }
    $pattern = '"bot_quota"\s+"[0-9]+"'
    if ([regex]::Matches($text,$pattern).Count -ne 1) { throw 'Expected exactly one panel bot_quota setting.' }
    $merged[$path] = [regex]::Replace($text,$pattern,'"bot_quota" "16"')
}
$backup = Join-Path $Csgo ('_botimprover_backups\v1.4.5-fairplay-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$paths = @($manifest.Files.Path) + @($merged.Keys) + @($binaryMerged.Keys) + @('gameinfo.gi','backup\Online\gameinfo.gi','backup\WithBots\gameinfo.gi')
if (@($paths | Select-Object -Unique).Count -ne $paths.Count) { throw 'Duplicate installer destination.' }
$records = @()
foreach ($relative in $paths) {
    Assert-RelativePath $relative
    $target = Join-Path $Csgo $relative
    $exists = Test-Path -LiteralPath $target -PathType Leaf
    if ((Test-Path -LiteralPath $target) -and !$exists) { throw ('Directory occupies file path: ' + $relative) }
    $hash = if ($exists) { (Get-FileHash -LiteralPath $target).Hash } else { $null }
    $records += [PSCustomObject]@{ Path=$relative; Existed=$exists; SHA256=$hash }
}
$parked = @()
foreach ($relative in $manifest.ParkPaths) {
    $target = [IO.Path]::GetFullPath((Join-Path $Csgo $relative))
    $saved = [IO.Path]::GetFullPath((Join-Path $backup ('parked\' + $relative)))
    if (!$target.StartsWith($Csgo + '\',[StringComparison]::OrdinalIgnoreCase) -or !$saved.StartsWith($backup + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Move escaped the selected game/backup folder.' }
    if (Test-Path -LiteralPath $target) { $parked += [PSCustomObject]@{ Path=$relative } }
}
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($record in $records) {
    if ($record.Existed) {
        $saved = Join-Path $backup ('files\' + $record.Path)
        New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $Csgo $record.Path) -Destination $saved
        if ((Get-FileHash -LiteralPath $saved).Hash -ne $record.SHA256) { throw ('Backup copy mismatch: ' + $record.Path) }
    }
}
[PSCustomObject]@{ Files=$records; Parked=$parked } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backup 'restore-manifest.json') -Encoding UTF8
$moved = @()
try {
    foreach ($record in $parked) {
        $saved = Join-Path $backup ('parked\' + $record.Path)
        New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
        Move-Item -LiteralPath (Join-Path $Csgo $record.Path) -Destination $saved
        $moved += $record
    }
    foreach ($entry in $manifest.Files) {
        $target = Join-Path $Csgo $entry.Path
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $payload $entry.Path) -Destination $target
        if ((Get-FileHash -LiteralPath $target).Hash -ne $entry.SHA256) { throw ('Copy mismatch: ' + $entry.Path) }
    }
    foreach ($path in $binaryMerged.Keys) { [IO.File]::WriteAllBytes((Join-Path $Csgo $path), $binaryMerged[$path]) }
    foreach ($path in $merged.Keys) { [IO.File]::WriteAllText((Join-Path $Csgo $path), $merged[$path], [Text.UTF8Encoding]::new($false)) }
    foreach ($mode in @('Online','WithBots')) { New-Item -ItemType Directory -Path (Join-Path $Csgo ('backup\' + $mode)) -Force | Out-Null }
    [IO.File]::WriteAllText((Join-Path $Csgo 'backup\Online\gameinfo.gi'), $templates.Online, $encoding)
    [IO.File]::WriteAllText((Join-Path $Csgo 'backup\WithBots\gameinfo.gi'), $templates.Bots, $encoding)
    [IO.File]::WriteAllText($gameinfoPath, $templates.Bots, $encoding)
} catch {
    foreach ($record in $records) {
        $target = Join-Path $Csgo $record.Path
        if ($record.Existed) { Copy-Item -LiteralPath (Join-Path $backup ('files\' + $record.Path)) -Destination $target }
        elseif (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target }
    }
    foreach ($record in $moved) { Move-Item -LiteralPath (Join-Path $backup ('parked\' + $record.Path)) -Destination (Join-Path $Csgo $record.Path) }
    throw
}
Write-Output ('Installed v1.4.5-fairplay.2 successfully. Backup: ' + $backup)
Write-Output ('Start the panel with: ' + (Join-Path $Csgo 'Start-CompatiblePanel.cmd'))
