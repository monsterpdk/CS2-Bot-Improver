param([Parameter(Mandatory)][string]$Original,[Parameter(Mandatory)][string]$FlashCorrected,[Parameter(Mandatory)][string]$Final)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$settingsPath=Join-Path $root 'compatibility\BotStateCompatibilitySettings.json'
$settings=Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
Import-Module (Join-Path $root 'compatibility\BotStateCompatibility.psm1') -Force
$fixture=Join-Path $PSScriptRoot ('test-output\botstate-'+[Guid]::NewGuid().ToString('N'))
$target=Join-Path $fixture $settings.Path
New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
foreach($inputPath in @($Original,$FlashCorrected,$Final)) {
    Copy-Item -LiteralPath $inputPath -Destination $target
    $updates=Get-BotStateCompatibilityUpdates -Csgo $fixture -SettingsPath $settingsPath
    $sha=[Security.Cryptography.SHA256]::Create()
    try { $hash=[BitConverter]::ToString($sha.ComputeHash([byte[]]$updates[$settings.Path])).Replace('-','') } finally { $sha.Dispose() }
    if($hash -ne $settings.SHA256) { throw 'Wrong adapter output.' }
}
[IO.File]::WriteAllBytes($target,[byte[]](1,2,3))
$rejected=$false
try { Get-BotStateCompatibilityUpdates -Csgo $fixture -SettingsPath $settingsPath | Out-Null } catch { $rejected=$_.Exception.Message -like '*Unsupported BotState binary*' }
if(!$rejected) { throw 'Unknown input was accepted.' }
Write-Output 'PASS original/flash-corrected/final BotState inputs converge to exact output; unknown input refused.'
