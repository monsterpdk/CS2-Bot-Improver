$ErrorActionPreference='Stop'
function Get-BytesSHA256 {
 param([byte[]]$Bytes)
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{return ([BitConverter]::ToString($algorithm.ComputeHash($Bytes))).Replace('-','')}finally{$algorithm.Dispose()}
}
function Assert-TestedGame {
 param([string]$Csgo,[string]$SettingsPath)
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 if(@($s.Files | Where-Object {$_}).Count){throw 'This release has no native binary adapters; install original v1.4.10/CSS381.'}
 if((Get-Content -LiteralPath (Join-Path $Csgo 'steam.inf') -Raw) -notmatch ('(?m)^ClientVersion='+[regex]::Escape($s.ClientVersion)+'\s*$')){throw 'Audited ClientVersion changed.'}
 foreach($e in @(@('bin\win64\server.dll',$s.ServerSHA256),@('..\bin\win64\engine2.dll',$s.EngineSHA256),@('..\bin\win64\networksystem.dll',$s.NetworkSHA256))){
  if((Get-FileHash -LiteralPath (Join-Path $Csgo $e[0])).Hash -ne $e[1]){throw ('Audited game binary changed: '+$e[0])}
 }
}
function Test-NativeCompatibilityHash {
 param([string]$Relative,[string]$Hash,[string]$Expected,[string]$SettingsPath)
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 if(@($s.Files | Where-Object {$_}).Count){throw 'This release has no native binary adapters.'}
 foreach($f in @($s.DataFiles | Where-Object {$_})){
  if($f.Path -eq $Relative -and $f.InputSHA256 -eq $Expected -and ($Hash -eq $f.OutputSHA256 -or $Hash -in @($f.AcceptedOutputSHA256))){return $true}
 }
 return $false
}
function Get-NativeCompatibilityUpdates {
 param([string]$Csgo,[string]$SettingsPath)
 Assert-TestedGame -Csgo $Csgo -SettingsPath $SettingsPath
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 $updates=@{}
 foreach($f in @($s.DataFiles | Where-Object {$_})){
  if(!$f.Path -or [IO.Path]::IsPathRooted($f.Path) -or $f.Path -match ':|(^|[/\\])\.\.([/\\]|$)'){throw 'Unsafe data adapter path.'}
  if($updates.ContainsKey($f.Path)){throw 'Duplicate adapter destination.'}
  [byte[]]$bytes=[IO.File]::ReadAllBytes((Join-Path $Csgo $f.Path))
  $hash=Get-BytesSHA256 $bytes
  $encoding=[Text.UTF8Encoding]::new($false,$true)
  $text=$encoding.GetString($bytes)
  $data=$text.TrimStart([char]0xFEFF) | ConvertFrom-Json
  $signature=$data.($f.SignatureKey).signatures.windows
  if($hash -eq $f.OutputSHA256 -or $hash -in @($f.AcceptedOutputSHA256)){
   if($signature -cne $f.AfterSignature){throw 'Corrected data signature differs.'}
   $updates[$f.Path]=$bytes;continue
  }
  if($hash -ne $f.InputSHA256){throw ('Unknown data baseline: '+$f.Path)}
  if($signature -cne $f.BeforeSignature -or [regex]::Matches($text,[regex]::Escape($f.BeforeSignature)).Count -ne 1){throw 'Data signature mismatch.'}
  [byte[]]$changed=$encoding.GetBytes($text.Replace($f.BeforeSignature,$f.AfterSignature))
  if((Get-BytesSHA256 $changed) -ne $f.OutputSHA256){throw 'Data adapter output mismatch.'}
  $updates[$f.Path]=$changed
 }
 return $updates
}
Export-ModuleMember -Function Assert-TestedGame,Test-NativeCompatibilityHash,Get-NativeCompatibilityUpdates
