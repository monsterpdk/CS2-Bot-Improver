param([string]$Baseline,[string]$CompatibleBaseline)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $root 'compatibility/NativeCompatibility.psm1') -Force
$dir=Join-Path $PSScriptRoot ('test-output\native-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$csgo=Join-Path $dir 'game/csgo'
New-Item -ItemType Directory -Path $csgo -Force | Out-Null
function Hash([byte[]]$Bytes){$a=[Security.Cryptography.SHA256]::Create();try{return ([BitConverter]::ToString($a.ComputeHash($Bytes))).Replace('-','')}finally{$a.Dispose()}}
function Assert($Condition,[string]$Message){if(!$Condition){throw $Message}}
function Hex([string]$Text){return ,([byte[]]@($Text.Split(' ') | ForEach-Object {[Convert]::ToByte($_,16)}))}
$settings=Get-Content -LiteralPath (Join-Path $root 'compatibility/NativeCompatibilitySettings.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$originalSettings= $settings | ConvertTo-Json -Depth 30
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value ('ClientVersion='+$settings.ClientVersion) -Encoding ASCII
foreach($pair in @(@('bin/win64/server.dll','ServerSHA256'),@('../bin/win64/engine2.dll','EngineSHA256'),@('../bin/win64/networksystem.dll','NetworkSHA256'))){
 $path=Join-Path $csgo $pair[0]
 New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
 [IO.File]::WriteAllBytes($path,[Text.Encoding]::ASCII.GetBytes($pair[0]))
 $settings.($pair[1])=(Get-FileHash -LiteralPath $path).Hash
}
if(!$Baseline){
 $sample=[byte[]](0x48,0x8b,0x01,0xff,0x50,0x10)
 $changed=[byte[]](0x48,0x8b,0xc1,0x90,0x90,0x90)
 $settings.DataFiles=@()
 $settings.Files=@([PSCustomObject]@{Path='addons/native-test.dll';InputSHA256=(Hash $sample);OutputSHA256=(Hash $changed);Edits=@([PSCustomObject]@{Name='descriptor identity';FileOffset=0;Before='48 8b 01 ff 50 10';After='48 8b c1 90 90 90'})})
}
$specs=@($settings.Files)+@($settings.DataFiles | Where-Object {$_})
$before=@{}
foreach($f in $specs){
 $dest=Join-Path $csgo $f.Path
 New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
 if($Baseline){Copy-Item -LiteralPath (Join-Path $Baseline $f.Path) -Destination $dest}else{[IO.File]::WriteAllBytes($dest,$sample)}
 $before[$f.Path]=[IO.File]::ReadAllBytes($dest)
 Assert ((Get-FileHash -LiteralPath $dest).Hash -eq $f.InputSHA256) 'Fixture baseline hash mismatch.'
}
$config=Join-Path $dir 'settings.json'
function Save-Settings {$settings | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $config -Encoding UTF8}
function Reject([string]$Contains){$failed=$false;try{$null=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config}catch{$failed=$_.Exception.Message.Contains($Contains)};Assert $failed ('Expected rejection: '+$Contains)}
Save-Settings
$updates=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
Assert ($updates.Count -eq $specs.Count) 'Missing transformation.'
foreach($f in $specs){Assert ((Hash $updates[$f.Path]) -eq $f.OutputSHA256) 'Output hash differs.';Assert ((Get-FileHash -LiteralPath (Join-Path $csgo $f.Path)).Hash -eq $f.InputSHA256) 'Transformation wrote to game.'}
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value 'ClientVersion=9999999' -Encoding ASCII
Reject 'ClientVersion'
Set-Content -LiteralPath (Join-Path $csgo 'steam.inf') -Value ('ClientVersion='+$settings.ClientVersion) -Encoding ASCII
$saved=$settings.NetworkSHA256;$settings.NetworkSHA256='invalid';Save-Settings;Reject 'Audited game binary changed';$settings.NetworkSHA256=$saved;Save-Settings
$f=$settings.Files[0];$path=Join-Path $csgo $f.Path
[IO.File]::WriteAllBytes($path,[byte[]](1,2,3));Reject 'Unknown native baseline';[IO.File]::WriteAllBytes($path,$before[$f.Path])
$saved=$f.Edits[0].Before;$f.Edits[0].Before=($saved -replace '^..','00');Save-Settings;Reject 'instruction mismatch';$f.Edits[0].Before=$saved;Save-Settings
$saved=$f.OutputSHA256;$f.OutputSHA256='invalid';Save-Settings;Reject 'output mismatch';$f.OutputSHA256=$saved;Save-Settings
foreach($f in $settings.Files){
 foreach($variant in @($f.InputVariants | Where-Object {$_})){
  [byte[]]$intermediate=$before[$f.Path].Clone()
  $variantOffsets=@($variant.Edits | ForEach-Object {[int]$_.FileOffset})
  foreach($edit in $f.Edits | Where-Object {[int]$_.FileOffset -notin $variantOffsets}){
   [byte[]]$old=Hex $edit.Before
   [byte[]]$new=Hex $edit.After
   for($i=0;$i -lt $old.Length;$i++){Assert ($intermediate[$edit.FileOffset+$i] -eq $old[$i]) 'Variant preparation mismatch.'}
   [Array]::Copy($new,0,$intermediate,[int]$edit.FileOffset,$new.Length)
  }
  Assert ((Hash $intermediate) -eq $variant.SHA256) 'Intermediate Controller hash differs.'
  [IO.File]::WriteAllBytes((Join-Path $csgo $f.Path),$intermediate)
  Assert (Test-NativeCompatibilityHash -Relative $f.Path -Hash $variant.SHA256 -Expected $f.InputSHA256 -SettingsPath $config) 'Known voice-only Controller rejected.'
  $upgraded=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
  Assert ((Hash $upgraded[$f.Path]) -eq $f.OutputSHA256) 'Intermediate Controller upgrade differs.'
  Assert ((Get-FileHash -LiteralPath (Join-Path $csgo $f.Path)).Hash -eq $variant.SHA256) 'Variant preparation wrote to game.'
  $saved=$variant.Edits[0].Before;$variant.Edits[0].Before=($saved -replace '^..','00');Save-Settings;Reject 'instruction mismatch';$variant.Edits[0].Before=$saved;Save-Settings
  [IO.File]::WriteAllBytes((Join-Path $csgo $f.Path),$before[$f.Path])
 }
}
foreach($f in @($settings.DataFiles | Where-Object {$_})){
 $saved=$f.BeforeSignature;$f.BeforeSignature='invalid';Save-Settings;Reject 'Data signature mismatch';$f.BeforeSignature=$saved;Save-Settings
 $saved=$f.OutputSHA256;$f.OutputSHA256='invalid';Save-Settings;Reject 'Data adapter output mismatch';$f.OutputSHA256=$saved;Save-Settings
 $path=Join-Path $csgo $f.Path;$saved=[IO.File]::ReadAllBytes($path)
 try{[IO.File]::WriteAllText($path,'{}');Reject 'Unknown data baseline'}finally{[IO.File]::WriteAllBytes($path,$saved)}
}
foreach($f in $specs){[IO.File]::WriteAllBytes((Join-Path $csgo $f.Path),$updates[$f.Path]);Assert (Test-NativeCompatibilityHash -Relative $f.Path -Hash $f.OutputSHA256 -Expected $f.InputSHA256 -SettingsPath $config) 'Adapted baseline rejected.';Assert (!(Test-NativeCompatibilityHash -Relative $f.Path -Hash $f.OutputSHA256 -Expected 'unrelated' -SettingsPath $config)) 'Unrelated expected dependency accepted.'}
$again=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
foreach($f in $specs){Assert ((Hash $again[$f.Path]) -eq $f.OutputSHA256) 'Reinstall is not idempotent.'}
Write-Output ('PASS native/data transformation, no-write behavior, version/network/baseline/instruction/output guards, exact dependency allowance and idempotency; files='+$settings.Files.Count)

if($CompatibleBaseline){
 foreach($f in $specs){
  $path=Join-Path $csgo $f.Path
  Copy-Item -LiteralPath (Join-Path $CompatibleBaseline $f.Path) -Destination $path
  $hash=(Get-FileHash -LiteralPath $path).Hash
  Assert ($hash -eq $f.InputSHA256 -or (Test-NativeCompatibilityHash -Relative $f.Path -Hash $hash -Expected $f.InputSHA256 -SettingsPath $config)) 'Compatible fixture has unknown input.'
 }
 $adapted=Get-NativeCompatibilityUpdates -Csgo $csgo -SettingsPath $config
 foreach($f in $specs){Assert ((Hash $adapted[$f.Path]) -eq (Get-FileHash -LiteralPath (Join-Path $csgo $f.Path)).Hash) 'Accepted repaired installation should remain unchanged.'}
 Write-Output 'PASS repaired local installation including alternate corrected gamedata is accepted without writes.'
}
