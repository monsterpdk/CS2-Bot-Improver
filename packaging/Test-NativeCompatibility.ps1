param([string]$Baseline,[string]$CompatibleBaseline)
if($CompatibleBaseline -and !$Baseline){throw "CompatibleBaseline requires an original Baseline."}
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $root 'compatibility/NativeCompatibility.psm1') -Force
$settings=Get-Content -LiteralPath (Join-Path $root 'compatibility/NativeCompatibilitySettings.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Assert($ok,[string]$message){if(!$ok){throw $message}}
Assert (@($settings.Files).Count -eq 0) 'Retired native adapters remain.'
$dir=Join-Path $PSScriptRoot ('test-output/gamedata-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$csgo=Join-Path $dir 'game/csgo';New-Item -ItemType Directory -Path $csgo -Force | Out-Null
$encoding=[Text.UTF8Encoding]::new($false)
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value ('ClientVersion='+$settings.ClientVersion) -Encoding ASCII
foreach($pair in @(@('bin/win64/server.dll','ServerSHA256'),@('../bin/win64/engine2.dll','EngineSHA256'),@('../bin/win64/networksystem.dll','NetworkSHA256'))){
 $path=Join-Path $csgo $pair[0];New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
 [IO.File]::WriteAllText($path,'synthetic '+$pair[0]);$settings.($pair[1])=(Get-FileHash -LiteralPath $path).Hash
}
$config=Join-Path $dir 'settings.json'
function Save{$settings | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $config -Encoding UTF8}
function Reject([string]$message){$rejected=$false;try{$null=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config}catch{$rejected=$_.Exception.Message.Contains($message)};Assert $rejected ('Expected rejection: '+$message)}
# CI has no licensed upstream pack. Build a disposable minimal source only when
# no baseline was requested; explicit baselines retain the exact published pins.
if(!$Baseline){
 $Baseline=Join-Path $dir 'synthetic-source'
 $relative='addons/counterstrikesharp/gamedata/gamedata.json'
 $path=Join-Path $Baseline $relative
 New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
 $sourceText="{`r`n  `"Fixture`": { `"signatures`": { `"windows`": `"BEFORE_SIGNATURE`", `"linux`": `"preserve_linux`" } },`r`n  `"OtherField`": `"preserve me`"`r`n}`r`n"
 [byte[]]$original=$encoding.GetBytes($sourceText)
 [byte[]]$corrected=$encoding.GetBytes($sourceText.Replace('BEFORE_SIGNATURE','AFTER_SIGNATURE'))
 function FixtureHash([byte[]]$Bytes){$a=[Security.Cryptography.SHA256]::Create();try{return ([BitConverter]::ToString($a.ComputeHash($Bytes))).Replace('-','')}finally{$a.Dispose()}}
 [IO.File]::WriteAllBytes($path,$original)
 $settings.DataFiles=@([PSCustomObject]@{
  Path=$relative;InputSHA256=(FixtureHash $original);OutputSHA256=(FixtureHash $corrected)
  AcceptedOutputSHA256=@();SignatureKey='Fixture';BeforeSignature='BEFORE_SIGNATURE';AfterSignature='AFTER_SIGNATURE'
 })
 Write-Output 'Using self-contained synthetic gamedata fixture; real upstream hash verification requires -Baseline.'
}
$before=@{}
foreach($f in $settings.DataFiles){
 $target=Join-Path $csgo $f.Path;New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $Baseline $f.Path) -Destination $target
 $before[$f.Path]=[IO.File]::ReadAllBytes($target);Assert ((Get-FileHash -LiteralPath $target).Hash -eq $f.InputSHA256) 'Wrong original data fixture.'
}
Save
$updates=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
Assert ($updates.Count -eq $settings.DataFiles.Count) 'Wrong data-update count.'
foreach($f in $settings.DataFiles){
 $hash=(Get-FileHash -LiteralPath (Join-Path $csgo $f.Path)).Hash;Assert ($hash -eq $f.InputSHA256) 'Preparation wrote to source.'
 $a=[Security.Cryptography.SHA256]::Create();try{$result=([BitConverter]::ToString($a.ComputeHash($updates[$f.Path]))).Replace('-','')}finally{$a.Dispose()}
 Assert ($result -eq $f.OutputSHA256) 'Narrowed output differs.'
 $text=$encoding.GetString($updates[$f.Path]);Assert ($text.Replace($f.AfterSignature,$f.BeforeSignature) -ceq $encoding.GetString($before[$f.Path])) 'Unrelated gamedata text changed.'
}
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value 'ClientVersion=0' -Encoding ASCII;Reject 'ClientVersion'
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value ('ClientVersion='+$settings.ClientVersion) -Encoding ASCII
$saved=$settings.NetworkSHA256;$settings.NetworkSHA256='invalid';Save;Reject 'Audited game binary changed';$settings.NetworkSHA256=$saved;Save
$settings.Files=@([PSCustomObject]@{Path='old-native.dll'});Save;Reject 'no native binary adapters';$settings.Files=@();Save
$f=$settings.DataFiles[0];$path=Join-Path $csgo $f.Path
$saved=$f.BeforeSignature;$f.BeforeSignature='invalid';Save;Reject 'Data signature mismatch';$f.BeforeSignature=$saved;Save
$saved=$f.OutputSHA256;$f.OutputSHA256='invalid';Save;Reject 'Data adapter output mismatch';$f.OutputSHA256=$saved;Save
try{[IO.File]::WriteAllText($path,'{}');Reject 'Unknown data baseline'}finally{[IO.File]::WriteAllBytes($path,$before[$f.Path])}
foreach($f in $settings.DataFiles){
 [IO.File]::WriteAllBytes((Join-Path $csgo $f.Path),$updates[$f.Path])
 Assert (Test-NativeCompatibilityHash -Relative $f.Path -Hash $f.OutputSHA256 -Expected $f.InputSHA256 -SettingsPath $config) 'Corrected data dependency rejected.'
 Assert (!(Test-NativeCompatibilityHash -Relative $f.Path -Hash $f.OutputSHA256 -Expected 'unrelated' -SettingsPath $config)) 'Unrelated expected dependency accepted.'
}
$again=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
foreach($f in $settings.DataFiles){Assert ([Convert]::ToBase64String($again[$f.Path]) -ceq [Convert]::ToBase64String($updates[$f.Path])) 'Reinstall changes corrected data.'}
Assert (!(Test-NativeCompatibilityHash -Relative 'addons/counterstrikesharp/bin/win64/counterstrikesharp.dll' -Hash 'adapted' -Expected 'original' -SettingsPath $config)) 'Native binary allowance remains.'
if($CompatibleBaseline){
 foreach($f in $settings.DataFiles){Copy-Item -LiteralPath (Join-Path $CompatibleBaseline $f.Path) -Destination (Join-Path $csgo $f.Path)}
 $current=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
 foreach($f in $settings.DataFiles){Assert ([Convert]::ToBase64String($current[$f.Path]) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $csgo $f.Path)))) 'Accepted local data changed.'}
}
Write-Output 'PASS data-only preparation; exact field/text preservation; version/network/input/metadata/output guards; no native adapters; idempotency and accepted local state.'
