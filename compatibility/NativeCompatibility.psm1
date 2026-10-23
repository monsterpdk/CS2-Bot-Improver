$ErrorActionPreference='Stop'
function Assert-TestedGame {
    param([string]$Csgo,[string]$SettingsPath)
    $settings=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($entry in @(@('bin\win64\server.dll',$settings.ServerSHA256),@('..\bin\win64\engine2.dll',$settings.EngineSHA256))) {
        $target=Join-Path $Csgo $entry[0]
        if(!(Test-Path -LiteralPath $target -PathType Leaf) -or (Get-FileHash -LiteralPath $target).Hash -ne $entry[1]) { throw ('Audited game binary changed: '+$entry[0]) }
    }
}
function Get-NativeCompatibilityUpdates {
    param([string]$Csgo,[string]$SettingsPath)
    $settings=Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $updates=@{}
    $target=Join-Path $Csgo $settings.Path
    $hash=(Get-FileHash -LiteralPath $target).Hash
    $bytes=[IO.File]::ReadAllBytes($target)
    if($hash -eq $settings.ExpectedCurrent) {
        # Translate RVAs using this exact PE's section table. No downloaded DLL.
        $pe=[BitConverter]::ToInt32($bytes,0x3c)
        if([BitConverter]::ToUInt32($bytes,$pe) -ne 0x4550) { throw 'Invalid native PE signature.' }
        $sections=[BitConverter]::ToUInt16($bytes,$pe+6)
        $table=$pe+24+[BitConverter]::ToUInt16($bytes,$pe+20)
        foreach($patch in $settings.Patches) {
            $rva=[Convert]::ToInt32($patch.RVA.Substring(2),16)
            $offset=-1
            for($i=0;$i -lt $sections;$i++) {
                $section=$table+40*$i
                $address=[BitConverter]::ToUInt32($bytes,$section+12)
                $size=[BitConverter]::ToUInt32($bytes,$section+16)
                if($rva -ge $address -and $rva -lt $address+$size) { $offset=[int]([BitConverter]::ToUInt32($bytes,$section+20)+$rva-$address); break }
            }
            if($offset -lt 0) { throw 'Native patch outside file-backed PE sections.' }
            $before=@($patch.Before.Split(' ') | ForEach-Object { [Convert]::ToByte($_,16) })
            $after=@($patch.After.Split(' ') | ForEach-Object { [Convert]::ToByte($_,16) })
            if($before.Count -ne $after.Count -or $offset+$before.Count -gt $bytes.Length) { throw 'Invalid native patch size.' }
            for($i=0;$i -lt $before.Count;$i++) {
                if($bytes[$offset+$i] -ne $before[$i]) { throw 'Native instruction does not match the audited original.' }
                $bytes[$offset+$i]=$after[$i]
            }
        }
        $sha=[Security.Cryptography.SHA256]::Create()
        try { $result=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','') } finally { $sha.Dispose() }
        if($result -ne $settings.SHA256) { throw 'Native adapter output hash mismatch.' }
    } elseif($hash -ne $settings.SHA256) { throw 'Unsupported CounterStrikeSharp native binary; use the original v1.4.5 CSS 376 bundle.' }
    $updates[$settings.Path]=$bytes
    $visionPath='addons\BotVision\gamedata.json'
    $vision=Get-Content -LiteralPath (Join-Path $Csgo $visionPath) -Raw -Encoding UTF8 | ConvertFrom-Json
    $signature=$vision.GetSmokeDensityInLine.signatures.windows
    if($signature -notin @($settings.SmokeSignatureBefore,$settings.SmokeSignatureAfter)) { throw 'Unexpected BotVision smoke signature.' }
    $vision.GetSmokeDensityInLine.signatures.windows=$settings.SmokeSignatureAfter
    $updates[$visionPath]=[Text.UTF8Encoding]::new($false).GetBytes(($vision | ConvertTo-Json -Depth 40))
    return $updates
}
Export-ModuleMember -Function Assert-TestedGame,Get-NativeCompatibilityUpdates
