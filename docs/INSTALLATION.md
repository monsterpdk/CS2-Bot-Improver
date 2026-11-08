# Installation and restore

Requires Windows x64, CS2 ClientVersion 2000942 / patch 1.41.9.6 and the author's original v1.4.5 Windows bot pack with bundled CSS 376. The updater checks exact server/engine binaries and pinned dependencies; it does not install on another build. Obtain upstream dependencies from the projects credited in NOTICE.md.

1. Extract the entire public update outside the game folder. Close CS2 and the panel.
2. Run Install.cmd. Enter the full target path ending in `game\csgo`.
3. The installer verifies payload hashes, dependencies and game binaries; backs up every changed file; applies managed plugins, gameinfo/profile/config updates, the local CSS engine adapter, BotState flash/recovery adapter and BotVision signature. It rolls back copied/merged files on failure.
4. Start `game\csgo\Start-CompatiblePanel.cmd` and select Bot Mode. Test a few rounds. Keep `bv_smoke_mode 1`.

The original panel executable is not in the public update; obtain it from upstream. Do not upload the separate complete personal-use bundle as a public release asset. Detailed diagnostics default to off; see [diagnostic switches and storage limits](DIAGNOSTICS.md).

Population defaults to 16 total (8v8 including a human); Medium/High retain their reaction settings. The public updater preserves unrelated panel preferences and active difficulty. It merges a specific native smoke signature while preserving other BotVision data. The native adapter accepts only the exact original or already-adapted CSS binary and verifies its resulting hash.

Backups are under `game\csgo\_botimprover_backups\v1.4.5-fairplay.4-<timestamp>`. Close CS2/panel, then run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "D:\...\game\csgo\_botimprover_backups\v1.4.5-fairplay.4-<timestamp>"
```

Use the exact backup folder printed by installation. Restore validates saved hashes before changing files. It restores only that transaction, not unrelated prior changes. The installer refuses unexpected managed plugin folders. Bot Mode uses -insecure; use the wrapper and Online mode before normal online play. Future CS2 updates require compatibility review. A second PC has not yet been playtested.
