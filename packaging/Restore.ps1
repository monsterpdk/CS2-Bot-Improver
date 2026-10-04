param([Parameter(Mandatory)][string]$Backup)
$ErrorActionPreference = 'Stop'
if (Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq 'cs2' -or $_.ProcessName -like 'Panel v*' }) { throw 'Close the game and panel first.' }
$Backup = (Resolve-Path -LiteralPath $Backup).Path.TrimEnd('\')
$backupRoot = Split-Path $Backup -Parent
if ([IO.Path]::GetFileName($backupRoot) -ne '_botimprover_backups') { throw 'Select the v1.4.5 backup timestamp folder.' }
$Csgo = Split-Path $backupRoot -Parent
if ([IO.Path]::GetFileName($Csgo) -ne 'csgo') { throw 'Invalid game backup folder.' }
$manifest = Get-Content -LiteralPath (Join-Path $Backup 'restore-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($record in @($manifest.Files) + @($manifest.Parked)) {
    if ([IO.Path]::IsPathRooted($record.Path) -or $record.Path -match '(^|[/\\])\.\.([/\\]|$)|:') { throw 'Unsafe restore path.' }
}
foreach ($record in $manifest.Files) {
    if ($record.Existed -and (!(Test-Path -LiteralPath (Join-Path $Backup ('files\' + $record.Path)) -PathType Leaf) -or (Get-FileHash -LiteralPath (Join-Path $Backup ('files\' + $record.Path))).Hash -ne $record.SHA256)) { throw ('Missing backup file: ' + $record.Path) }
}
foreach ($record in $manifest.Parked) {
    if (!(Test-Path -LiteralPath (Join-Path $Backup ('parked\' + $record.Path))) -or (Test-Path -LiteralPath (Join-Path $Csgo $record.Path))) { throw ('Cannot safely restore parked path: ' + $record.Path) }
}
foreach ($record in $manifest.Files) {
    $target = Join-Path $Csgo $record.Path
    if ($record.Existed) { New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null; Copy-Item -LiteralPath (Join-Path $Backup ('files\' + $record.Path)) -Destination $target }
    elseif (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target }
}
foreach ($record in $manifest.Parked) {
    $saved = [IO.Path]::GetFullPath((Join-Path $Backup ('parked\' + $record.Path)))
    $target = [IO.Path]::GetFullPath((Join-Path $Csgo $record.Path))
    if (!$saved.StartsWith($Backup + '\',[StringComparison]::OrdinalIgnoreCase) -or !$target.StartsWith($Csgo + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Restore move escaped the backup/game folder.' }
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Move-Item -LiteralPath $saved -Destination $target
}
Write-Output ('Installation restored from: ' + $Backup)
