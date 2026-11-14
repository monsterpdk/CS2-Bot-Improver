# Build and reproduce

Requirements: Windows PowerShell, .NET 10 SDK, NuGet access, and a separately installed exact original v1.4.8/CSS 379 pack. The six projects target CounterStrikeSharp.API 1.0.379. BotState also needs the original pinned BotControllerApi shared DLL. Build-Plugins verifies and copies that local reference into ignored libs; it is not a public release asset.

```powershell
.\packaging\Build-Plugins.ps1 -Csgo "FULL_GAME_CSGO_FOLDER"
.\packaging\Build-Update.ps1 -Csgo "FULL_GAME_CSGO_FOLDER" -Output "NEW_OUTPUT_FOLDER" -UseBuiltPlugins
```

The scripts build/package only, without installing into the game. Output source excludes build/bin/obj/libs/logs, executables, PDBs and private dumps. The update manifest records all payload hashes and required upstream dependency hashes. Source ZIPs are developer artifacts, not installers.

Run policy/diagnostic tests from tests/*/*.csproj. CompiledChecks additionally takes the SDK api directory, original BotControllerApi.dll, built BotState.dll and built BotAI.dll as four arguments. Run packaging/Test-Package.ps1 -Package <staged-update> -Baseline <original-v1.4.8-folder> for isolated version/hash guards, rollback, install, reinstall and restore checks. The fixture replaces game binary hashes only inside its disposable copy. Original source/data provenance and exact upstream commits are recorded in data/ and UPSTREAM-SOURCES.json.

Full builds retain upstream obsolete ConVar warnings; passing automated tests does not prove collision geometry or stability on every map. Public opt-in logging differs from the local test defaults. Stable promotion follows extended local gameplay including High (Hard); no separate live test of the clean public build is claimed.

Run `packaging/Test-NativeCompatibility.ps1` for synthetic native transformation guards, or add `-Baseline <original-v1.4.8-folder>` to verify both real upstream DLLs against the published output hashes without modifying that baseline. The native DLLs are generated locally during installation, not included in the update payload.
