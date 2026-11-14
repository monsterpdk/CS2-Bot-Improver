# Installation and restore

Prerequisite: install original **v1.4.9 Windows**, including bundled CSS 380 and updated Metamod/Panel/BotHider. A v1.4.8 Fairplay installation must be upgraded to that original baseline first. Target: CS2 ClientVersion 2000943 / patch 1.41.9.7, exact pinned dependencies.

Extract this release outside game/csgo, close CS2 and panel, run Install.cmd and select game/csgo. The updater rejects unknown binaries/profiles/plugins, merges current gameinfo/layer paths, preserves difficulty selection and preferences, generates humanized seven-entry Medium/High VPKs including retake reactions, and sets fill quota 16 (8v8 including players). Low stays original upstream. It applies only the BotController native adapter; official CSS 380 stays unmodified. Obsolete RayTrace and BotHider whitelist paths are parked when present.

Eight managed plugins and their source/data/configs are included. The original runtime/native/shared libraries, panel and VPK assets are obtained separately. All changes are backed up under game/csgo/_botimprover_backups/v1.4.9-fairplay.1-TIMESTAMP. Copy failures roll back. Reinstall is supported.

With game and panel closed, use this update's Restore.ps1:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "FULL_BACKUP_FOLDER"
```

Restore returns to the state immediately before this update, not before an earlier original-pack upgrade or Steam update. Future builds need review; do not bypass hash/version guards.
