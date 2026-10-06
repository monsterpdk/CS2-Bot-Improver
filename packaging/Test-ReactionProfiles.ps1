param([Parameter(Mandatory)][string]$Baseline)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $root 'compatibility\ProfileCompatibility.psm1') -Force
$settingsPath = Join-Path $root 'compatibility\ReactionSettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$fixture = Join-Path $PSScriptRoot ('test-output\profiles-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
foreach ($level in @('Low','Medium','High')) {
    $dest = Join-Path $fixture ('overrides\' + $level)
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $Baseline ('overrides\' + $level + '\botprofile.vpk')) -Destination $dest
}
function Assert($Condition,[string]$Message) { if (!$Condition) { throw $Message } }
foreach ($selected in @('Low','Medium','High')) {
    $active = Join-Path $fixture 'overrides\botprofile.vpk'
    Copy-Item -LiteralPath (Join-Path $fixture ('overrides\' + $selected + '\botprofile.vpk')) -Destination $active
    $before = (Get-FileHash -LiteralPath $active).Hash
    $updates = Get-ProfileUpdates -Csgo $fixture -SettingsPath $settingsPath
    Assert ($updates.ContainsKey('overrides\botprofile.vpk') -eq ($selected -ne 'Low')) 'Wrong active selection handling.'
    foreach ($path in $updates.Keys) { [IO.File]::WriteAllBytes((Join-Path $fixture $path),$updates[$path]) }
    foreach ($level in @('Medium','High')) { Assert ((Get-FileHash -LiteralPath (Join-Path $fixture ('overrides\' + $level + '\botprofile.vpk'))).Hash -eq $settings.$level.OutputSHA256) 'Generated profile hash differs.' }
    if ($selected -eq 'Low') { Assert ((Get-FileHash -LiteralPath $active).Hash -eq $before) 'Low active profile changed.' }
    else { Assert ((Get-FileHash -LiteralPath $active).Hash -eq $settings.$selected.OutputSHA256) 'Active preset not preserved.' }
    $again = Get-ProfileUpdates -Csgo $fixture -SettingsPath $settingsPath
    foreach ($path in $again.Keys) {
        $hash = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($again[$path])).Replace('-','')
        Assert ($hash -eq (Get-FileHash -LiteralPath (Join-Path $fixture $path)).Hash) 'Reinstall not idempotent.'
    }
}
$path = Join-Path $fixture 'overrides\Medium\botprofile.vpk'
$bytes = [IO.File]::ReadAllBytes($path)
try {
    [IO.File]::WriteAllBytes($path,[byte[]](1,2,3)); $rejected=$false
    try { Get-ProfileUpdates -Csgo $fixture -SettingsPath $settingsPath | Out-Null } catch { $rejected=$_.Exception.Message -like '*Unknown Medium profile*' }
    Assert $rejected 'Custom/corrupted profile accepted.'
} finally { [IO.File]::WriteAllBytes($path,$bytes) }
Write-Output 'PASS: exact accepted hashes, Low/Medium/High selection, unchanged Low, repeat install and custom/corrupted-profile guard.'
