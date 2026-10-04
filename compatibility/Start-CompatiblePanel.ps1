param(
    [string]$Csgo,
    [string]$PanelExe
)
$ErrorActionPreference = 'Stop'
try {
    if (!$Csgo) {
        $Csgo = if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'gameinfo.gi')) { $PSScriptRoot } else { Read-Host 'CS2 game\csgo folder (full path)' }
    }
    if (!$PanelExe) { $PanelExe = Join-Path $Csgo 'Panel v1.4.5.exe' }
    $panelPath = (Resolve-Path -LiteralPath $PanelExe).Path
    & (Join-Path $PSScriptRoot 'Sync-PanelGameInfo.ps1') -Csgo $Csgo
    Start-Process -FilePath $panelPath -WorkingDirectory $Csgo -WindowStyle Normal
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
