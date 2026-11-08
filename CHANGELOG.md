# v1.4.5-fairplay.4 ? 2026-11-08

Compatibility and fairness update for Windows local bot matches on **CS2 ClientVersion 2000942 / patch 1.41.9.6**, original Bot Improver v1.4.5 and CounterStrikeSharp 376.

- Scripted grenade throws now consume actual carried grenades. Bots buy real inventory using their money at the round boundary and may use picked-up grenades. No team-wide allowance or throw-time payment can create another grenade. Carry caps are one smoke/HE/fire/decoy, at most two flashes and four grenades total, subject to the game's configured limits. Molotov/incendiary items retain their real identity.
- All regular, plant/defuse and emergency grenade paths share inventory consumption. Existing launch position, range, speed, heading, pitch and obstruction checks remain. Fixes the empty-item removal receiver and the ammo-array network notification that produced unresolved offset messages.
- Bots retain native flash blindness. A local BotState adapter removes artificial immunity that could cancel native blind calls or erase flash events. BotAimImprover holds a fixed pre-blind aim point during its blindness guard instead of allowing live target tracking. Blind fire and chance hits remain possible.
- Updates the audited native hooks/patch checks for the tested game. The installer validates exact game/runtime files and derives CSS/BotState compatibility changes from the user's own original DLLs; those dependencies are not redistributed.
- Repairs two BotState recovery faults: continuous jump-timer rearming and a one-attempt-only recovery latch. Existing sidestep/repath recovery may retry after 3?5 seconds without progress, while combat/visible enemies and defusing suppress intervention. This is an **initially tested recovery improvement**, not a guarantee against every navigation stall or team-wide deadlock.
- Adds an opt-in stationary observer, including repeated in-place jumping. It records evidence without changing bot behavior.
- Detailed inventory, flash and observer logging is **off by default**. Each module can keep two 1 MiB detail files and two 128 KiB error files. Across all three modules: at most **6 MiB detail + 768 KiB own error logs**, with fixed filenames across sessions. No personal trial logs are distributed. Engine, CounterStrikeSharp and other upstream logs have their own retention policies.

Retains fair smoke aiming, delayed/approximate sound awareness, smoke movement assistance, teammate spacing, humanized Medium/High reaction profiles and 8v8 defaults from fairplay.3. Decoy use still depends on recorded map lineups; owning a decoy does not create a useful lineup automatically.

## Validation

Local gameplay verified inventory consumption, positive flash durations, absence of the prior unresolved ammo offsets and an initial recovery after about 4?5 seconds. The original mass Mirage stall's initiating cause remains unproven. Release sources differ from the accepted local gameplay sources only in logging/defaults and version labels; these release logging defaults have automated tests but have not had a separate live playtest. Policy/build tests and clean install, reinstall, injected failure rollback and restore checks pass. Another PC, Linux and public/dedicated servers are not validated.

Install the original v1.4.5 Windows pack first, then extract **CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-11-08.zip** outside the game and run Install.cmd with CS2/panel closed. Launch Start-CompatiblePanel.cmd. Earlier Fairplay installs can use the same updater if all dependency checks pass. A future game update requires a compatibility review; do not bypass hash/version guards.

---

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
