param([string]$Csgo)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'GameInfoCompatibility.psm1') -Force
function Assert-True($condition, $message) { if (!$condition) { throw $message } }
$fresh = "`"GameInfo`"`n{`n title `"future title { with braces }`"`n // SearchPaths { fake comment }`n FileSystem`n {`n SearchPaths`n {`n Game csgo`n Game core`n Mod csgo`n }`n }`n FutureSetting `"preserve me`"`n}`n"
$result = New-PanelGameInfoTemplates -Text $fresh
Assert-True ($result.Online -ceq $fresh) 'Online changed a fresh stock file.'
Assert-True ($result.Bots.Contains("Game`tcsgo/addons/metamod")) 'Bots has no mount.'
Assert-True ($result.Bots.Contains('FutureSetting "preserve me"')) 'Future settings were lost.'
$again = New-PanelGameInfoTemplates -Text $result.Bots
Assert-True ($again.Bots -ceq $result.Bots) 'Bots transformation is not idempotent.'
Assert-True ($again.Online -notmatch '(?m)^\s*Game\s+csgo/addons/metamod') 'Online retained the mount.'
$quoted = $fresh.Replace('Game csgo', '"Game" "csgo\addons\metamod" // managed mount' + "`nGame csgo")
$quotedResult = New-PanelGameInfoTemplates -Text $quoted
Assert-True ($quotedResult.Bots -ceq $quoted) 'Existing quoted mount was altered.'
Assert-True (!$quotedResult.Online.Contains('// managed mount')) 'Quoted mount was not removed.'
$duplicate = $result.Bots.Replace("Game`tcsgo/addons/metamod", "Game`tcsgo/addons/metamod`nGame csgo/addons/metamod")
$deduped = New-PanelGameInfoTemplates -Text $duplicate
Assert-True ([regex]::Matches($deduped.Bots, 'csgo/addons/metamod').Count -eq 1) 'Duplicate mounts remain.'
$profiles = New-PanelGameInfoTemplates -Text $fresh -BotProfiles
Assert-True ($profiles.Bots.Contains('csgo/overrides/botprofile.vpk')) 'Profile mount missing.'
Assert-True (!$profiles.Online.Contains('csgo/overrides/botprofile.vpk')) 'Profile mounted in Online.'
$profilesAgain = New-PanelGameInfoTemplates -Text $profiles.Bots -BotProfiles
Assert-True ($profilesAgain.Bots -ceq $profiles.Bots) 'Profile mount is not idempotent.'
Assert-True ($profilesAgain.Online -ceq $profiles.Online) 'Profile modes do not round-trip.'
$profilesOff = New-PanelGameInfoTemplates -Text $profiles.Bots
Assert-True (!$profilesOff.Bots.Contains('csgo/overrides/botprofile.vpk')) 'Disabled profile mount remained.'
$profilesDuplicate = New-PanelGameInfoTemplates -Text ($profiles.Bots.Replace("Game`tcsgo/overrides/botprofile.vpk", "Game`tcsgo/overrides/botprofile.vpk`nGame csgo/overrides/botprofile.vpk")) -BotProfiles
Assert-True ([regex]::Matches($profilesDuplicate.Bots, 'csgo/overrides/botprofile.vpk').Count -eq 1) 'Duplicate profile mounts remain.'
$quotedProfile = $profiles.Bots.Replace("Game`tcsgo/overrides/botprofile.vpk", '"Game" "csgo\overrides\botprofile.vpk" // profile')
$quotedProfiles = New-PanelGameInfoTemplates -Text $quotedProfile -BotProfiles
Assert-True ($quotedProfiles.Bots -ceq $quotedProfile) 'Quoted profile mount changed.'
$failed = $false
try { New-PanelGameInfoTemplates -Text $fresh.Substring(0, $fresh.Length - 3) | Out-Null } catch { $failed = $true }
Assert-True $failed 'Unbalanced braces were accepted.'
$layered = New-PanelGameInfoTemplates -Text ($fresh.Replace('FutureSetting', 'LayeredOnMod csgo_imported' + "`nFutureSetting"))
Assert-True ($layered.Layers -contains 'csgo_imported') 'Layer dependency was not detected.'
$currentPath = if ($Csgo) { Join-Path $Csgo 'gameinfo.gi' } else { $null }
if ($currentPath -and (Test-Path -LiteralPath $currentPath)) {
    $current = [IO.File]::ReadAllText($currentPath)
    $actual = New-PanelGameInfoTemplates -Text $current
    $actualAgain = New-PanelGameInfoTemplates -Text $actual.Bots
    Assert-True ($actualAgain.Bots -ceq $actual.Bots) 'Installed gameinfo Bot Mode is not idempotent.'
    Assert-True ($actualAgain.Online -ceq $actual.Online) 'Installed gameinfo modes do not round-trip.'
}
Write-Output 'PASS: stock preservation, future settings, quoted braces/comments, idempotence, quoted mounts, duplicates, malformed structure, layer detection and current stable gameinfo.'
