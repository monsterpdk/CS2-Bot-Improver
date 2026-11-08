$ErrorActionPreference='Stop'
function Get-BotStateCompatibilityUpdates {
    param([string]$Csgo,[string]$SettingsPath)
    $settings=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $target=Join-Path $Csgo $settings.Path
    if(!(Test-Path -LiteralPath $target -PathType Leaf)) { throw 'BotState dependency missing. Install the original v1.4.5 Windows pack first.' }
    $hash=(Get-FileHash -LiteralPath $target).Hash
    if($hash -notin $settings.AllowedInputSHA256) { throw 'Unsupported BotState binary; use the original v1.4.5 bundle.' }
    $bytes=[IO.File]::ReadAllBytes($target)
    if($hash -ne $settings.SHA256) {
        foreach($patch in $settings.Patches) {
            $offset=[int]$patch.Offset
            $before=[byte[]]@($patch.Before.Split(' ') | ForEach-Object { [Convert]::ToByte($_,16) })
            $after=[byte[]]@($patch.After.Split(' ') | ForEach-Object { [Convert]::ToByte($_,16) })
            if($offset -lt 0 -or $before.Length -ne $after.Length -or $offset+$before.Length -gt $bytes.Length) { throw 'Invalid BotState patch bounds.' }
            for($i=0;$i -lt $before.Length;$i++) {
                if($bytes[$offset+$i] -ne $before[$i] -and $bytes[$offset+$i] -ne $after[$i]) { throw 'Unexpected BotState method bytes.' }
                $bytes[$offset+$i]=$after[$i]
            }
        }
    }
    $sha=[Security.Cryptography.SHA256]::Create()
    try { $output=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','') } finally { $sha.Dispose() }
    if($output -ne $settings.SHA256) { throw 'BotState adapter output hash mismatch.' }
    $updates=@{};$updates[$settings.Path]=$bytes;return $updates
}
Export-ModuleMember -Function Get-BotStateCompatibilityUpdates
