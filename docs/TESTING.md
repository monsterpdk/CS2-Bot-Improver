# Testing

Run the self-contained policy projects under tests with dotnet run --project <project>. They cover inventory transactions, flash, smoke aim, throw safety, awareness, idle tracking, bounded diagnostics and crouch/detour geometry.

CompiledChecks loads built BotState/BotAI and the real SDK/shared API to validate lifecycle guards, native timing preservation, disabled bypasses and owned-input cleanup; see BUILDING.md for arguments.

Installer fixtures:

```powershell
.\packaging\Test-Package.ps1 -Package "STAGED_UPDATE_FOLDER" -Baseline "EXTRACTED_ORIGINAL_V148_FOLDER"
.\packaging\Test-ReactionProfiles.ps1 -Baseline "EXTRACTED_ORIGINAL_V148_FOLDER"
```

Tests operate on disposable copies, with synthetic game binary hashes only in the fixture. Do not use the live game as a fixture output. Cover exact version/dependency guards, damaged payload, extra plugins, injected copy failure rollback, settings/profile selection preservation, install/reinstall and restore. Policy passes do not prove every map collision or crash cause.
