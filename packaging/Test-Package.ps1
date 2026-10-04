param([Parameter(Mandatory)][string]$Package, [string]$Baseline)
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path $PSScriptRoot ('test-output\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$fixture = Join-Path $testRoot 'game\csgo'
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
if ($Baseline) { foreach ($item in Get-ChildItem -LiteralPath $Baseline -Force) { Copy-Item -LiteralPath $item.FullName -Destination $fixture -Recurse } }
function Get-Process { param($ErrorAction) return @() } # Test fixture only.
function Assert($Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Expect-Rejection([string]$Message) {
    $rejected = $false
    try { & (Join-Path $Package 'Install.ps1') -Csgo $fixture } catch { $rejected = $_.Exception.Message -like ('*' + $Message + '*') }
    Assert $rejected ('Expected rejection: ' + $Message)
}
function Snapshot {
    $hashes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $fixture -Recurse -File) {
        $relative = $file.FullName.Substring($fixture.Length + 1)
        if ($relative -notlike '_botimprover_backups\*') { $hashes[$relative] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    }
    return $hashes
}
function Assert-Snapshot($Expected) {
    $actual = Snapshot
    Assert ($actual.Count -eq $Expected.Count) 'Snapshot file count changed.'
    foreach ($path in $Expected.Keys) { Assert ($actual[$path] -eq $Expected[$path]) ('Snapshot mismatch: ' + $path) }
}
$manifest = Get-Content -LiteralPath (Join-Path $Package 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$stock = "GameInfo`r`n{`r`n FutureSetting preserve`r`n FileSystem`r`n {`r`n SearchPaths`r`n {`r`n Game csgo`r`n Game core`r`n }`r`n }`r`n}`r`n"
[IO.File]::WriteAllText((Join-Path $fixture 'gameinfo.gi'), $stock)
Set-Content -LiteralPath (Join-Path $fixture 'steam.inf') -Value 'ClientVersion=9999999' -Encoding ASCII
Expect-Rejection 'Target version differs'
Assert (!(Test-Path -LiteralPath (Join-Path $fixture '_botimprover_backups'))) 'Version guard wrote a backup.'
Set-Content -LiteralPath (Join-Path $fixture 'steam.inf') -Value ('ClientVersion=' + $manifest.ClientVersion) -Encoding ASCII
if ($manifest.Dependencies.Count) {
    $dependency = Join-Path $fixture $manifest.Dependencies[0].Path
    $bytes = [IO.File]::ReadAllBytes($dependency)
    try { [IO.File]::WriteAllBytes($dependency, [byte[]](1,2,3)); Expect-Rejection 'Dependency missing or different' }
    finally { [IO.File]::WriteAllBytes($dependency, $bytes) }
    Assert (!(Test-Path -LiteralPath (Join-Path $fixture '_botimprover_backups'))) 'Dependency guard wrote a backup.'
}
$foreign = Join-Path $fixture 'addons\counterstrikesharp\plugins\ForeignPlugin'
New-Item -ItemType Directory -Path $foreign -Force | Out-Null
try { Expect-Rejection 'Additional plugin found' } finally { Remove-Item -LiteralPath $foreign }
if ($manifest.MergeCoreSettings) {
    $corePath = Join-Path $fixture 'addons\counterstrikesharp\configs\core.json'
    $core = Get-Content -LiteralPath $corePath -Raw | ConvertFrom-Json
    $core | Add-Member -NotePropertyName FutureOption -NotePropertyValue 'preserve'
    $core | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $corePath -Encoding UTF8
}
$before = Snapshot
& (Join-Path $Package 'Install.ps1') -VerifyOnly
$payloadPath = Join-Path $Package ('payload\' + $manifest.Files[0].Path)
$bytes = [IO.File]::ReadAllBytes($payloadPath)
try { [IO.File]::WriteAllBytes($payloadPath, [byte[]](1,2,3)); Expect-Rejection 'Package damaged' }
finally { [IO.File]::WriteAllBytes($payloadPath, $bytes) }
Assert-Snapshot $before
function Copy-Item {
    param([string]$LiteralPath, [string]$Destination)
    if ($LiteralPath -like '*\payload\*' -and $Destination -like '*\NadeSystem.dll') { throw 'Injected copy failure' }
    Microsoft.PowerShell.Management\Copy-Item -LiteralPath $LiteralPath -Destination $Destination
}
try { Expect-Rejection 'Injected copy failure' } finally { Remove-Item Function:\Copy-Item }
Assert-Snapshot $before
& (Join-Path $Package 'Install.ps1') -Csgo $fixture
$successfulBackup = Get-ChildItem -LiteralPath (Join-Path $fixture '_botimprover_backups') -Directory | Sort-Object Name | Select-Object -Last 1
foreach ($entry in $manifest.Files) { Assert ((Get-FileHash -LiteralPath (Join-Path $fixture $entry.Path)).Hash -eq $entry.SHA256) ('Installed mismatch: ' + $entry.Path) }
Assert ([IO.File]::ReadAllText((Join-Path $fixture 'backup\Online\gameinfo.gi')) -ceq $stock) 'Online template did not preserve target gameinfo.'
Assert ([IO.File]::ReadAllText((Join-Path $fixture 'gameinfo.gi')).Contains('csgo/overrides/botprofile.vpk')) 'Profile mount missing.'
foreach ($relative in $manifest.ParkPaths) { Assert (!(Test-Path -LiteralPath (Join-Path $fixture $relative))) ('Old path still active: ' + $relative) }
if ($manifest.MergeCoreSettings) {
    $core = Get-Content -LiteralPath $corePath -Raw | ConvertFrom-Json
    Assert ($core.FutureOption -eq 'preserve' -and !$core.AutoUpdateEnabled -and !$core.FollowCS2ServerGuidelines) 'Core merge incorrect.'
}
$installed = Snapshot
& (Join-Path $Package 'Install.ps1') -Csgo $fixture
$reinstallBackup = Get-ChildItem -LiteralPath (Join-Path $fixture '_botimprover_backups') -Directory | Sort-Object Name | Select-Object -Last 1
& (Join-Path $Package 'Restore.ps1') -Backup $reinstallBackup.FullName
Assert-Snapshot $installed
$backupFile = $before.Keys | Where-Object { Test-Path -LiteralPath (Join-Path $successfulBackup.FullName ('files\' + $_)) } | Select-Object -First 1
$saved = Join-Path $successfulBackup.FullName ('files\' + $backupFile)
$bytes = [IO.File]::ReadAllBytes($saved)
$rejected = $false
try {
    [IO.File]::WriteAllBytes($saved, [byte[]](1,2,3))
    try { & (Join-Path $Package 'Restore.ps1') -Backup $successfulBackup.FullName } catch { $rejected = $_.Exception.Message -like '*Missing backup file*' }
    Assert $rejected 'Corrupted backup was not rejected.'
    Assert-Snapshot $installed
} finally { [IO.File]::WriteAllBytes($saved, $bytes) }
& (Join-Path $Package 'Restore.ps1') -Backup $successfulBackup.FullName
Assert-Snapshot $before
Write-Output 'PASS: version/dependency/extra-plugin and integrity guards, failed-copy rollback, install hashes, gameinfo, core preservation, reinstall, corrupt-backup guard and full restore.'
Write-Output ('Disposable fixture: ' + $fixture)
