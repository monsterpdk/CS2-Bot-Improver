# Installation and restore

Prerequisite: install original **v1.4.9 Windows**, including bundled CSS 380 and updated Metamod/Panel/BotHider. A v1.4.8 Fairplay installation must be upgraded to that original baseline first. Target: CS2 ClientVersion 2000950 / patch 1.42.0.1, exact pinned dependencies.

Extract this release outside game/csgo, close CS2 and panel, run Install.cmd and select game/csgo. The updater rejects unknown binaries/profiles/plugins, merges current gameinfo/layer paths, preserves difficulty selection and preferences, generates humanized seven-entry Medium/High VPKs including retake reactions, and sets fill quota 16 (8v8 including players). Low stays original upstream. It locally adapts CSS 380, BotHider and BotController and narrows the weapon-removal signature, accepting only known original/previous/repaired hashes. Obsolete RayTrace and BotHider whitelist paths are parked when present.

Eight managed plugins and their source/data/configs are included. The original runtime/native/shared libraries, panel and VPK assets are obtained separately. All changes are backed up under game/csgo/_botimprover_backups/v1.4.9-fairplay.3-TIMESTAMP. Copy failures roll back. Reinstall is supported.

With game and panel closed, use this update's Restore.ps1:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "FULL_BACKUP_FOLDER"
```

Restore returns to the state immediately before this update, not before an earlier original-pack upgrade or Steam update. Future builds need review; do not bypass hash/version guards.

Existing v1.4.9-fairplay.1/.2 and the accepted local 2000950 repair can upgrade directly. The previous voice-only Controller is transformed without applying its voice edits twice. The native/data checks also accept already-final inputs for reinstall. Unknown/custom gamedata is rejected.
