$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
foreach ($name in @('BotAI','BotAimImprover','NadeSystem','BotVisionCompatibility')) {
    $project = Join-Path $root ('addons\counterstrikesharp\plugins\' + $name + '\' + $name + '.csproj')
    & dotnet build $project -c Release -o (Join-Path $root ('build\' + $name))
    if ($LASTEXITCODE -ne 0) { throw ('Build failed: ' + $name) }
}
Write-Output 'Four maintained plugins built for API 376. Nothing installed into the game.'
