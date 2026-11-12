param([Parameter(Mandatory)][string]$Csgo)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$shared=Join-Path $Csgo 'addons\counterstrikesharp\shared\BotControllerApi\BotControllerApi.dll'
$pins=Get-Content (Join-Path $root 'data/UPSTREAM-DEPENDENCIES.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$pin=($pins | Where-Object Path -eq 'addons\counterstrikesharp\shared\BotControllerApi\BotControllerApi.dll').SHA256
if((Get-FileHash -LiteralPath $shared).Hash -ne $pin){throw 'Pinned shared API changed.'}
$libs=Join-Path $root 'addons/counterstrikesharp/plugins/BotState/libs'
New-Item -ItemType Directory -Path $libs -Force | Out-Null
Copy-Item -LiteralPath $shared -Destination (Join-Path $libs 'BotControllerApi.dll')
foreach ($name in @('BotAI','BotAimImprover','NadeSystem','BotVisionCompatibility','BotStuckDiagnostics','BotState')) {
    $project = Join-Path $root ('addons\counterstrikesharp\plugins\' + $name + '\' + $name + '.csproj')
    & dotnet build $project -c Release -o (Join-Path $root ('build\' + $name))
    if ($LASTEXITCODE -ne 0) { throw ('Build failed: ' + $name) }
}
Write-Output 'Six maintained plugins built for API 379. Nothing installed into the game.'
