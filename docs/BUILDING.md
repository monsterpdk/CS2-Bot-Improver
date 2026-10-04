# Build the four maintained plugins

Use Windows and .NET SDK 10 with NuGet access (tested SDK 10.0.401). From this repository's root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File packaging\Build-Plugins.ps1
```

The script builds BotAI, BotAimImprover, NadeSystem and BotVisionCompatibility against pinned CounterStrikeSharp.API 1.0.376 into `build/<name>/`. It does not install anything. Corridor traces use CSS's built-in API, so no RayTrace API reference DLL is needed. BotRandomizer and the panel are obtained unchanged from upstream and are not rebuilt here.

Distribute only the four named plugin DLL/deps pairs, not NuGet/runtime dependency DLLs from the build output. The maintained corresponding source, notices, data and build tools accompany the public binaries. Bit-identical binaries from a different source path/SDK are not promised.

To stage a new update from a matching tested installation:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File packaging\Build-Update.ps1 -Csgo "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo" -Output "D:\Releases\fairplay-stage"
```

This checks the pinned dependency hashes in `data/UPSTREAM-DEPENDENCIES.json`, selects the maintained plugin binaries/data, repository cfg files, source, docs and wrapper, and creates a manifest. Review version metadata and dependencies before releasing another game build. No original panel/runtime/native binaries or VPKs are selected.
