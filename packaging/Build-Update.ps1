param([Parameter(Mandatory)][string]$Csgo, [Parameter(Mandatory)][string]$Output, [switch]$UseBuiltPlugins)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Choose a new output folder.' }
if ((Get-Content -LiteralPath (Join-Path $Csgo 'steam.inf') -Raw) -notmatch '(?m)^ClientVersion=2000943\s*$') { throw 'Build only from the tested ClientVersion 2000943 installation.' }
$dependencies = Get-Content -LiteralPath (Join-Path $root 'data\UPSTREAM-DEPENDENCIES.json') -Raw | ConvertFrom-Json
$dependencies = @($dependencies | ForEach-Object { [PSCustomObject]@{ Path=$_.Path; SHA256=$_.SHA256 } })
Import-Module (Join-Path $root 'compatibility/NativeCompatibility.psm1') -Force
Assert-TestedGame -Csgo $Csgo -SettingsPath (Join-Path $root 'compatibility/NativeCompatibilitySettings.json')
foreach ($entry in $dependencies) {
    if ((Get-FileHash -LiteralPath (Join-Path $Csgo $entry.Path)).Hash -ne $entry.SHA256 -and !(Test-NativeCompatibilityHash -Relative $entry.Path -Hash (Get-FileHash -LiteralPath (Join-Path $Csgo $entry.Path)).Hash -Expected $entry.SHA256 -SettingsPath (Join-Path $root 'compatibility/NativeCompatibilitySettings.json'))) { throw ('Pinned dependency changed: ' + $entry.Path) }
}
if(!$UseBuiltPlugins) { & (Join-Path $PSScriptRoot 'Build-Plugins.ps1') -Csgo $Csgo }
$payload = Join-Path $Output 'payload'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
function Copy-Payload([string]$Source, [string]$Relative) {
    $target = Join-Path $payload $Relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $target
}
foreach ($name in @('BotAI','BotAimImprover','NadeSystem','BotVisionCompatibility','BotStuckDiagnostics','BotState')) {
    foreach ($suffix in @('.dll','.deps.json')) {
        $relative = 'addons\counterstrikesharp\plugins\' + $name + '\' + $name + $suffix
        Copy-Payload (Join-Path $root ('build\'+$name+'\'+$name+$suffix)) $relative
    }
    $source = Join-Path $root ('addons\counterstrikesharp\plugins\' + $name)
    foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File -Filter '*.json') {
        if ($file.FullName -match '[\\/](bin|obj|libs)[\\/]') { continue }
        $relative = $file.FullName.Substring($root.Length + 1)
        Copy-Payload $file.FullName $relative
    }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'cfg') -File) { Copy-Payload $file.FullName ('cfg\' + $file.Name) }
foreach ($name in @('Start-CompatiblePanel.cmd','Start-CompatiblePanel.ps1','Sync-PanelGameInfo.ps1','GameInfoCompatibility.psm1','CompatibilitySettings.json','ProfileCompatibility.psm1','ReactionSettings.json','DefaultServerConfig.vdf','NativeCompatibility.psm1','NativeCompatibilitySettings.json')) { Copy-Payload (Join-Path $root ('compatibility\' + $name)) $name }
foreach ($name in @('Install.ps1','Install.cmd','Restore.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $Output $name) }
foreach ($name in @('README.md','CHANGELOG.md','NOTICE.md','LICENSE')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $Output $name) }
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $Output -Recurse
$source = Join-Path $Output 'source'
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Force) {
    $relative = $file.FullName.Substring($root.Length + 1)
    if ($relative -match '(^|[\\/])(\.git|bin|obj|libs|build|dist|test-output)[\\/]|\.(dll|pdb|zip|exe)$') { continue }
    $target = Join-Path $source $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
$files = @(Get-ChildItem -LiteralPath $payload -Recurse -File | ForEach-Object { [PSCustomObject]@{ Path=$_.FullName.Substring($payload.Length + 1); SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash } })
[PSCustomObject]@{
    Version='1.4.8-fairplay.3'; Prerelease=$false; ReleaseDate='2026-11-14'; ClientVersion='2000943'; PatchVersion='1.41.9.7'; Platform='Windows x64'; UpstreamBaseline='v1.4.8'; CounterStrikeSharp='1.0.379'
    Plugins=@('BotAI','BotAimImprover','BotBuy','BotControllerImpl','BotHiderImpl','BotRandomizer','BotState','BotStuckDiagnostics','BotVisionCompatibility','NadeSystem','RoundDamageRecap')
    Files=$files; Dependencies=$dependencies; ParkPaths=@('addons\metamod\RayTrace.vdf'); ApplyNativeCompatibility=$true; ApplyBotStateCompatibility=$false; MergeCoreSettings=$true; GenerateReactionProfiles=$true; MergePopulationSettings=$true
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Output 'manifest.json') -Encoding UTF8
Write-Output ('Public v1.4.8 update staged: ' + $files.Count + ' payload files.')
