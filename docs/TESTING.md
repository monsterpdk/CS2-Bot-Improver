# Testing

Run the self-contained policy projects under tests with dotnet run --project <project>. They cover inventory transactions, flash, smoke aim, throw safety, awareness, idle tracking, bounded diagnostics and crouch/detour geometry.

CompiledChecks loads built BotState/BotAI and the real SDK/shared API to validate lifecycle guards, native timing preservation, disabled bypasses and owned-input cleanup; see BUILDING.md for arguments.

Installer fixtures:

```powershell
.\packaging\Test-Package.ps1 -Package "STAGED_UPDATE_FOLDER" -Baseline "EXTRACTED_ORIGINAL_V1410_FOLDER"
.\packaging\Test-ReactionProfiles.ps1 -Baseline "EXTRACTED_ORIGINAL_V1410_FOLDER"
```

Tests operate on disposable copies, with synthetic game binary hashes only in the fixture. Do not use the live game as a fixture output. Cover exact version/dependency guards, damaged payload, extra plugins, injected copy failure rollback, settings/profile selection preservation, install/reinstall and restore. Policy passes do not prove every map collision or crash cause.

## GitHub Actions compatibility fixture

`./packaging/Test-NativeCompatibility.ps1` with no arguments builds a disposable synthetic gamedata source and checks data-only preparation, guards, preservation and idempotency without CS2/upstream binaries. This is the workflow's self-contained mode; it does not verify the real original pack hashes.

For exact v1.4.10 hash/output validation, run `./packaging/Test-NativeCompatibility.ps1 -Baseline "EXTRACTED_ORIGINAL_V1410_FOLDER"`. Optionally add `-CompatibleBaseline "ACCEPTED_LOCAL_V1410_CSGO"` to check the known corrected local data state. Both explicit paths remain read-only sources. Fixture-specific hash substitutions occur only in a disposable settings copy; published settings and installer guards are unchanged.
