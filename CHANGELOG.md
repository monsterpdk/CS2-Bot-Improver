# v1.4.5-fairplay.3 — 2026-10-23

Adds sound-aware behavior, help moving out of smoke, and spacing between teammates while preserving the earlier fair grenade/smoke changes, humanized Medium/High reaction profiles and 8v8 defaults.

- Bots can react to recent enemy footsteps and gunfire with delayed, approximate sound locations. Sound information expires and does not continuously track a hidden opponent. Native local hearing is restored; global hearing bypasses remain disabled.
- Idle bots in smoke can make short, collision-checked moves toward recent sound information, remembered bomb information, or an exit. Native navigation remains in charge; bots are not teleported.
- Nearby teammates can yield space rather than guard the exact same position. Humans and defusing teammates have priority.
- Fixes grenade selection stopping at an unusable first lineup. Other nearby legal lineups can now be considered. Launch position, speed, range, heading, pitch and obstruction limits remain enforced.
- Repairs smoke projectile creation and updates audited BotAI, BotAimImprover and BotVision signatures for CS2 ClientVersion 2000935.
- Defers awareness entity access until a live round, fixing unsafe startup initialization.
- Adapts three CSS 376 engine-interface call displacements for the tested Windows engine, fixing the pre-menu crash. Authentication checks and Steam identities are preserved.
- Adds on-demand `bot_fair_awareness status` and `bot_nades_status` counters, without periodic diagnostic logging.

Tested on Windows x64, CS2 **ClientVersion 2000935 / patch 1.41.9.2**, original v1.4.5 pack with CSS 376. The installer checks the exact server and engine binaries as well as dependencies. Another game build requires a compatibility review. Linux and a second PC have not been tested.

The user accepted the combined behavior and reported stable gameplay. The last grenade test recorded 44 smokes, 3 HE grenades and 10 flashes created and detonated, plus 8 created Molotovs. Molotov detonation is not counted. This does not guarantee every grenade type in every round or human-equivalent behavior in every situation.

**Public installation:** download `CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-10-23.zip`, extract it outside the game, close CS2/the panel, and run `Install.cmd` against `game\csgo`. Install the author's original v1.4.5 Windows pack first (or update an existing fairplay installation with its unchanged dependencies). The update patches the locally installed CSS binary after validating its hash; it does not distribute a replacement native DLL. Source code downloads are not installers.

Launch `game\csgo\Start-CompatiblePanel.cmd`, then Bot Mode. Default population remains 16 including the human player. Keep smoke mode 1. Restore instructions and limits are in the included documentation.


# 1.4.5-fairplay.2 — 2026-10-07

Humanized Medium/High reaction times; default 16-player fill population (8v8 including humans); public updater transforms installed profiles locally and preserves the active preset and other panel preferences. Tested CS2 ClientVersion 2000927 / patch 1.41.8.9. See [profile details](docs/REACTION-PROFILES.md).

# Changelog

## 1.4.5-fairplay.1 — 2026-10-04

- Rebase the accepted grenade and smoke fairness changes onto the actual v1.4.5 release-matching component sources, recorded in UPSTREAM-SOURCES.json. The root tag's older component pointers were not used for rebuilding the newer packaged DLLs.
- Retain upstream Rush, FOV, cosmetics, radio, T bomb defense, Rules panel and revised body/trunk aim priorities.
- Preserve grenade launch geometry/position safeguards, concealed-target aim guard, eye-height corridor blind fire, three disabled CT information bypasses and the Windows smoke visibility correction.
- Use CounterStrikeSharp API 376 and its built-in Trace API. Remove the old separate RayTrace bridge requirement.
- Retain upstream's updated grenade factories and native plugins. Do not reapply the old v1.4.4 native gamedata, BotAI/FOV or BotRandomizer fixes.
- Supply a pinned v1.4.5 public updater, current source and documentation; keep the original panel out of public assets.
- User accepted the combined local Windows playtest. Detailed grenade/smoke sampling logs remain disabled.

## Previous maintenance snapshot — 2026-10-03

The previous release used v1.4.4 / CSS 373 and repaired its compatibility before adding the accepted grenade and smoke changes. Its installer/publication archives remain historical snapshots and are not the v1.4.5 release.
