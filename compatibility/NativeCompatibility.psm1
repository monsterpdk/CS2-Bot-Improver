$ErrorActionPreference='Stop'
function Assert-TestedGame {
 param([string]$Csgo,[string]$SettingsPath)
 $s=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
 foreach($e in @(@('bin\win64\server.dll',$s.ServerSHA256),@('..\bin\win64\engine2.dll',$s.EngineSHA256))){
  if((Get-FileHash -LiteralPath (Join-Path $Csgo $e[0])).Hash -ne $e[1]){throw ('Audited game binary changed: '+$e[0])}
 }
}
Export-ModuleMember -Function Assert-TestedGame
