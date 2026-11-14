# Reaction times and match population — Fairplay 1.4.8-fairplay.3

Medium reaction templates now range from 0.30 to 0.45 seconds (default 0.35); High ranges from 0.22 to 0.35 seconds (default 0.28). Rush uses 0.35 / 0.26 seconds respectively. Low is unchanged. Accuracy, aim speed, focus, skill, attack delay and behavior trees are unchanged. These are engine profile settings, not a guaranteed minimum time between seeing a target and the first shot.

| Template | Medium (seconds) | High (seconds) |
|---|---:|---:|
| Default | 0.35 | 0.28 |
| ProTop | 0.30 | 0.22 |
| ProFast | 0.32 | 0.24 |
| ProPrecise | 0.34 | 0.26 |
| ProSlow | 0.40 | 0.32 |
| ProSteady | 0.36 | 0.28 |
| RankRifler | 0.42 | 0.34 |
| RankDuelist | 0.38 | 0.30 |
| RankOthers | 0.45 | 0.35 |

Normal team matches default to 16 players in fill mode: 8 per team, including humans. A solo player therefore has 7 friendly bots and 8 opponents. Free-for-all uses 16 total participants. The panel quota and Normal/FFA/Rush configs agree. Team assignment remains governed by the game's existing settings.

The public updater transforms the installed original v1.4.8 Medium/High VPKs locally with Windows PowerShell and preserves the active Low/Medium/High selection. No Python, SDK or separately downloaded VPK is required. Unknown/custom profiles are rejected before writing. Only reaction values change; CRC32 and all three VPK MD5 fields are rebuilt, and the complete output must match the accepted test's SHA-256. Reinstalling is supported. Profile files and panel settings join the installer's existing backup/rollback/restore transaction.

The private full installer includes the accepted presets and starts with Low. Original panel/runtime/profile assets remain excluded from the public release. Existing smoke and grenade fairness changes are retained.

Target: Windows x64, CS2 ClientVersion 2000943 / patch 1.41.9.7. Reaction profile transformations are unchanged; the native audit covers 32 enabled BotAI patches plus used hooks/factories, and the network descriptor adapter has separate fixture validation. All six public managed DLLs are rebuilt from the current source. Extended local gameplay, including High (Hard), has positive user feedback; this is not separate public-build, second-PC or all-map validation.
