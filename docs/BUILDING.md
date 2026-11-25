# Build and reproduce

Requirements: Windows PowerShell, .NET 10 SDK, NuGet access, and a separately installed exact original v1.4.10/CSS 381 pack. The eight projects target CounterStrikeSharp.API 1.0.381. BotState also needs the original pinned BotControllerApi shared DLL. Build-Plugins verifies and copies that local reference into ignored libs; it is not a public release asset.

```powershell
.\packaging\Build-Plugins.ps1 -Csgo "FULL_GAME_CSGO_FOLDER"
.\packaging\Build-Update.ps1 -Csgo "FULL_GAME_CSGO_FOLDER" -Output "NEW_OUTPUT_FOLDER" -UseBuiltPlugins
```

The scripts build/package only, without installing into the game. Output source excludes build/bin/obj/libs/logs, executables, PDBs and private dumps. The update manifest records all payload hashes and required upstream dependency hashes. Source ZIPs are developer artifacts, not installers.

Run policy/diagnostic tests from tests/*/*.csproj. CompiledChecks takes API directory, new BotControllerApi.dll, built State/AI/observer DLLs and BotAI gamedata.json (six arguments). Run packaging/Test-Package.ps1 -Package <staged-update> -Baseline <original-v1.4.10-folder> for isolated version/hash guards, rollback, install, reinstall and restore checks. The fixture replaces game binary hashes only inside its disposable copy. Original source/data provenance and exact upstream commits are recorded in data/ and UPSTREAM-SOURCES.json.

Full builds retain upstream obsolete ConVar warnings; passing automated tests does not prove collision geometry or stability on every map. Public opt-in logging differs from the local test defaults. The local compatibility build received positive gameplay feedback; no separate live test of the clean public build is claimed.

Run `packaging/Test-NativeCompatibility.ps1 -Baseline <original-v1.4.10-folder>` to verify the data-only signature update, guards, no-write preparation and idempotency. Add `-CompatibleBaseline <accepted-local-v1.4.10-csgo>` to test the known corrected data state. Both paths are read-only sources for disposable fixtures. No binary adapter source/settings are carried. CompiledChecks takes API directory, new shared API DLL, built State/AI/observer DLLs and BotAI gamedata.json (six arguments). BotAIRebaseTests takes API directory, built BotAI DLL and its gamedata.json.
