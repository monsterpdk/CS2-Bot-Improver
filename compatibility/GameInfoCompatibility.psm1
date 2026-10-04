Set-StrictMode -Version Latest

function Get-GameInfoTokens {
    param([Parameter(Mandatory)][string]$Text)
    $pattern = '"(?:\\.|[^"\\])*"|//[^\r\n]*|/\*[\s\S]*?\*/|[{}]|[^\s{}"]+'
    foreach ($match in [regex]::Matches($Text, $pattern)) {
        if ($match.Value.StartsWith('//') -or $match.Value.StartsWith('/*')) { continue }
        [PSCustomObject]@{ Value = $match.Value.Trim('"'); Index = $match.Index; Length = $match.Length; Raw = $match.Value }
    }
}

function Set-BotProfileMount {
    param([string]$Text, [bool]$Enabled)
    $tokens = @(Get-GameInfoTokens -Text $Text)
    $open = -1
    $close = -1
    $depth = 0
    for ($i = 0; $i -lt $tokens.Count; $i++) {
        if ($tokens[$i].Value -eq 'SearchPaths') { $open = $tokens[$i + 1].Index }
        if ($open -ge 0 -and $tokens[$i].Index -ge $open) {
            if ($tokens[$i].Raw -eq '{') { $depth++ }
            if ($tokens[$i].Raw -eq '}') { $depth--; if ($depth -eq 0) { $close = $tokens[$i].Index; break } }
        }
    }
    if ($open -lt 0 -or $close -le $open) { throw 'Invalid profile SearchPaths structure.' }
    $body = $Text.Substring($open + 1, $close - $open - 1)
    $pattern = '(?im)^[ \t]*"?Game"?[ \t]+"?csgo[/\\]overrides[/\\]botprofile\.vpk"?[ \t]*(?://[^\r\n]*)?(?:\r?\n|$)'
    $matches = [regex]::Matches($body, $pattern)
    $bodyTokens = @(Get-GameInfoTokens -Text $body)
    $count = 0
    for ($i = 0; $i + 1 -lt $bodyTokens.Count; $i++) {
        if ($bodyTokens[$i].Value -eq 'Game' -and $bodyTokens[$i + 1].Value.Replace('\', '/') -eq 'csgo/overrides/botprofile.vpk') { $count++ }
    }
    if ($matches.Count -ne $count) { throw 'Botprofile mount must occupy its own SearchPaths line.' }
    if ($Enabled -and $count -eq 1) { return $Text }
    $body = [regex]::Replace($body, $pattern, '')
    if ($Enabled) {
        $newline = if ($Text.Contains("`r`n")) { "`r`n" } else { "`n" }
        if (!$body.StartsWith("`n") -and !$body.StartsWith("`r`n")) { $body = $newline + $body }
        $body = $newline + "`t`t`tGame`tcsgo/overrides/botprofile.vpk" + $body
    }
    return $Text.Substring(0, $open + 1) + $body + $Text.Substring($close)
}

function New-PanelGameInfoTemplates {
    param([Parameter(Mandatory)][string]$Text, [switch]$BotProfiles)
    $tokens = @(Get-GameInfoTokens -Text $Text)
    $depth = 0
    $searchOpen = -1
    $searchClose = -1
    $searchDepth = -1
    $layers = @()
    for ($i = 0; $i -lt $tokens.Count; $i++) {
        $token = $tokens[$i]
        if ($token.Value -eq 'LayeredOnMod') {
            if ($i + 1 -ge $tokens.Count) { throw 'LayeredOnMod has no value.' }
            $layers += $tokens[$i + 1].Value
        }
        if ($token.Value -eq 'SearchPaths') {
            if ($searchOpen -ge 0) { throw 'Multiple SearchPaths blocks are unsupported.' }
            if ($i + 1 -ge $tokens.Count -or $tokens[$i + 1].Value -ne '{') { throw 'SearchPaths is not a block.' }
            $searchOpen = $tokens[$i + 1].Index
            $searchDepth = $depth + 1
        }
        if ($token.Raw -eq '{') { $depth++ }
        if ($token.Raw -eq '}') {
            if ($depth -eq $searchDepth -and $searchClose -lt 0) { $searchClose = $token.Index }
            $depth--
            if ($depth -lt 0) { throw 'Unbalanced gameinfo braces.' }
        }
    }
    if ($depth -ne 0 -or $searchOpen -lt 0 -or $searchClose -le $searchOpen) { throw 'Invalid gameinfo SearchPaths structure.' }
    $prefix = $Text.Substring(0, $searchOpen + 1)
    $body = $Text.Substring($searchOpen + 1, $searchClose - $searchOpen - 1)
    $suffix = $Text.Substring($searchClose)
    $pattern = '(?im)^[ \t]*"?Game"?[ \t]+"?csgo[/\\]addons[/\\]metamod"?[ \t]*(?://[^\r\n]*)?(?:\r?\n|$)'
    $matches = [regex]::Matches($body, $pattern)
    $bodyTokens = @(Get-GameInfoTokens -Text $body)
    $mountCount = 0
    for ($i = 0; $i + 1 -lt $bodyTokens.Count; $i++) {
        if ($bodyTokens[$i].Value -eq 'Game' -and $bodyTokens[$i + 1].Value.Replace('\', '/') -eq 'csgo/addons/metamod') { $mountCount++ }
    }
    if ($matches.Count -ne $mountCount) { throw 'Metamod mount must occupy its own SearchPaths line.' }
    $onlineBody = [regex]::Replace($body, $pattern, '')
    $online = $prefix + $onlineBody + $suffix
    if ($mountCount -eq 1) { $bots = $Text }
    else {
        $newline = if ($Text.Contains("`r`n")) { "`r`n" } else { "`n" }
        $remainingBody = if ($onlineBody.StartsWith("`n") -or $onlineBody.StartsWith("`r`n")) { $onlineBody } else { $newline + $onlineBody }
        $bots = $prefix + $newline + "`t`t`tGame`tcsgo/addons/metamod" + $remainingBody + $suffix
    }
    $online = Set-BotProfileMount -Text $online -Enabled $false
    $bots = Set-BotProfileMount -Text $bots -Enabled ([bool]$BotProfiles)
    [PSCustomObject]@{ Online = $online; Bots = $bots; Layers = $layers }
}

Export-ModuleMember -Function New-PanelGameInfoTemplates
