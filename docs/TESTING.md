# Validation

Run the pure policy checks from the repository root:

```powershell
dotnet run --project tests/SmokeAimTests/SmokeAimTests.csproj -c Release
dotnet run --project tests/ReplaySafetyTests/ReplaySafetyTests.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File compatibility\Test-GameInfoCompatibility.ps1
```

Installer checks use disposable fixtures only:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File packaging\Test-Package.ps1 -Package "D:\Releases\fairplay-stage" -Baseline "D:\Fixtures\original-v1.4.5"
```

Baseline is an extracted original v1.4.5 Windows pack, not the actual game folder. The test creates its own fixture and synthetic stock gameinfo/steam.inf, checks dependency/version/extra-plugin guards, corrupt payload rejection, injected copy-failure rollback, all installed hashes, core setting preservation, gameinfo, reinstall, corrupt backup rejection and complete restore. It mocks process enumeration only in the test scope.

Before installation, the integration audited 38 active Windows BotAI patch signatures/original bytes, aim fields, native smoke density and grenade factories against CS2 2000924. Untouched feature DLLs/Rush cfgs and upstream aim priorities were preserved. Those audits do not ship the inspected game binaries.

The user accepted the merged local Windows playtest before this release. A new native/aim/grenade change still requires gameplay checks for team selection, round transitions, smoke and affected throws. Dedicated, Linux, every map, Rush/radio/T-defense and bomb-search edge cases are not covered by that acceptance.
