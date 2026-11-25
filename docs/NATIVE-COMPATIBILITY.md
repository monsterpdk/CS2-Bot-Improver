# Compatibility - original v1.4.10 / API 381

Target: Windows CS2 2000950 / 1.42.0.1. Install exact original v1.4.10 native/API/shared/implementation files first. The retired CSS380/Hider/Controller/voice adapters are not carried into this release. NativeCompatibilitySettings.json has an empty Files array; its helper explicitly rejects attempts to reintroduce binary adapters. Native dependencies must match the original release hashes exactly.

Official BotAI 1.9.1 replaces the old patch implementation with JSON definitions, pre-write overlap checks, structured offsets and protected memory writes/restoration. The corrected bomb-search target is 0x2c94c0, the verified one-argument planted-site leaf getter. The former wrong-target safety exclusion is retired. Seven intentional fairness exclusions are filtered separately without modifying official JSON.

The only compatibility update generated locally is the Windows RemovePlayerItem signature text in gamedata. The original 381 pattern still matches two functions; a verified longer pattern uniquely selects the WeaponServices function. Original-to-corrected whole-file hashes are pinned, all unrelated original text stays unchanged, and the accepted local corrected JSON formatting is separately recognized unchanged. Unknown/custom data is rejected. Data preparation writes nothing before the main transaction. Reinstall is idempotent and restore verifies backup/current hashes.

Current evidence is in data/native-audit.json, official-native-audit.json, gamedata-decision.json, weapon-remove-evidence.json and REBASE-DECISIONS.json. Retired adapter byte settings/reports are omitted; previous releases preserve their history. The optional unused nav signature remains unavailable and is not replaced with an ambiguous pattern.

No native/game/runtime/shared DLL or panel is redistributed. The accepted private rebase completed a full Inferno match. Public managed binaries have different labels/disabled diagnostic defaults and no separate live acceptance. Future game versions require review.
