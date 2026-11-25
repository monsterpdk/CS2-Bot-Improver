# Installation and restore

Prerequisite: **original v1.4.10 Windows, bundled CSS 381, new native/shared/implementation modules and panel**. A v1.4.9 Fairplay installation must first upgrade to that original baseline. The accepted local v1.4.10 integration already satisfies it. Target: Windows CS2 2000950 / 1.42.0.1, exact dependency hashes.

Extract the Update ZIP outside game/csgo. Close CS2/panel, run Install.cmd and select game/csgo. Start Start-CompatiblePanel.cmd in Bot Mode. The installer verifies payload/game/dependencies/plugins, prepares only one narrow gamedata edit, generates current gameinfo mode templates and humanized profiles, preserves selected difficulty/preferences and sets fill quota 16 (8v8 including humans). Low and non-reaction profile contents remain upstream. No native DLL is transformed.

All replacements are backed up under game/csgo/_botimprover_backups/v1.4.10-fairplay.1-TIMESTAMP; partial failures roll back. Reinstall is supported. Detailed logs stay disabled/bounded; personal old logs are preserved. Source ZIPs are developer artifacts, not installers.

With game/panel closed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "FULL_BACKUP_FOLDER"
```

Restore returns to immediately before this update, not before the separately installed original upgrade. Retired old native adapters or other unknown dependencies/gamedata are rejected. Do not bypass future build/hash guards.
