$ErrorActionPreference='Stop'
function Get-BytesSHA256 {
 param([byte[]]$Bytes)
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{return ([BitConverter]::ToString($algorithm.ComputeHash($Bytes))).Replace('-','')}finally{$algorithm.Dispose()}
}
function Convert-NativeHex {
 param([string]$Text)
 return ,([byte[]]@($Text.Split(' ',[StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object {[Convert]::ToByte($_,16)}))
}
function Assert-TestedGame {
 param([string]$Csgo,[string]$SettingsPath)
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 if((Get-Content -LiteralPath (Join-Path $Csgo 'steam.inf') -Raw) -notmatch ('(?m)^ClientVersion='+[regex]::Escape($s.ClientVersion)+'\s*$')){throw 'Audited ClientVersion changed.'}
 foreach($e in @(@('bin\win64\server.dll',$s.ServerSHA256),@('..\bin\win64\engine2.dll',$s.EngineSHA256),@('..\bin\win64\networksystem.dll',$s.NetworkSHA256))){
  if((Get-FileHash -LiteralPath (Join-Path $Csgo $e[0])).Hash -ne $e[1]){throw ('Audited game binary changed: '+$e[0])}
 }
}
function Test-NativeCompatibilityHash {
 param([string]$Relative,[string]$Hash,[string]$Expected,[string]$SettingsPath)
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 foreach($f in $s.Files){if($f.Path -eq $Relative -and $f.InputSHA256 -eq $Expected -and $f.OutputSHA256 -eq $Hash){return $true}}
 return $false
}
function Get-NativeCompatibilityUpdates {
 param([string]$Csgo,[string]$SettingsPath)
 Assert-TestedGame -Csgo $Csgo -SettingsPath $SettingsPath
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 $updates=@{}
 foreach($f in $s.Files){
  if(!$f.Path -or [IO.Path]::IsPathRooted($f.Path) -or $f.Path -match ':|(^|[/\\])\.\.([/\\]|$)'){throw 'Unsafe native adapter path.'}
  if($updates.ContainsKey($f.Path)){throw 'Duplicate native adapter destination.'}
  [byte[]]$bytes=[IO.File]::ReadAllBytes((Join-Path $Csgo $f.Path))
  $hash=Get-BytesSHA256 $bytes
  if($hash -eq $f.OutputSHA256){$updates[$f.Path]=$bytes;continue}
  if($hash -ne $f.InputSHA256){throw ('Unknown native baseline: '+$f.Path)}
  $claimed=New-Object 'Collections.Generic.HashSet[int]'
  foreach($edit in $f.Edits){
   [byte[]]$before=Convert-NativeHex $edit.Before
   [byte[]]$after=Convert-NativeHex $edit.After
   $offset=[int]$edit.FileOffset
   if(!$before.Length -or $before.Length -ne $after.Length -or $offset -lt 0 -or $offset -gt ($bytes.Length-$before.Length)){throw 'Invalid native edit range.'}
   for($i=0;$i -lt $before.Length;$i++){
    if(!$claimed.Add($offset+$i) -or $bytes[$offset+$i] -ne $before[$i]){throw ('Native instruction mismatch: '+$edit.Name)}
   }
   [Array]::Copy($after,0,$bytes,$offset,$after.Length)
  }
  if((Get-BytesSHA256 $bytes) -ne $f.OutputSHA256){throw ('Native adapter output mismatch: '+$f.Path)}
  $updates[$f.Path]=$bytes
 }
 return $updates
}
Export-ModuleMember -Function Assert-TestedGame,Test-NativeCompatibilityHash,Get-NativeCompatibilityUpdates
