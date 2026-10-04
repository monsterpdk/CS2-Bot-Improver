# v1.4.5 fair-play port — 2026-10-04

Windows integration of the user-accepted grenade and smoke changes into the original v1.4.5 Windows release. This source tree is the maintained source for the new candidate; the older top-level plugin sources and previous publication archives remain historical snapshots.

## Baseline and provenance

- Original release: [ed0ard/CS2-Bot-Improver v1.4.5](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.5), root commit `d9914454d8691b509bae4cd671a7a8bd9dcb174f`.
- Actual packaged versions: BotAI 1.8.12, BotAimImprover 2.1.5, NadeSystem 1.2.2, CounterStrikeSharp API 376. Several submodule pointers in the root release tag referenced older sources; this port uses the release-matching newer component commits recorded in `UPSTREAM-SOURCES.json`.
- Game audited: CS2 ClientVersion 2000924, patch 1.41.8.8, Windows x64. The port's own smoke guard is Windows-only. No claim is made that its additions have been tested on Linux or dedicated servers.

## Added on top of the new release

- NadeSystem **1.2.2-fairplay.1**: retain the new native factory signatures; add the previous release-offset, range, speed, heading, pitch, finite-value and obstruction limits. Re-anchor replay and immediate grenade launches to the actual bot eye position, including delayed NextFrame validation. Apply replay safety before spending money for ordinary and retaliation throws and before committing decoy replays. Do not port the old diagnostic counters/logging.
- BotAimImprover **2.1.5-fairplay.1**: retain the upstream body/trunk/mixed priorities and offsets. Add instruction validation, schema-based bot resolution, the native-density smoke aim guard, remembered visible points, limited blind-fire pitch, and fixed eye-level directions favoring open corridors. Adapt corridor probes to CounterStrikeSharp's built-in Trace API instead of the removed external RayTrace capability.
- BotAI **1.8.12-fairplay.1**: retain all upstream definitions; exclude the same three CT bomb visibility/information bypasses and add the same Windows both-endpoints-inside-smoke correction. The old FOV-removal definitions are not restored.
- BotVisionCompatibility **0.1.4-fairplay.1**: retain automatic mode 1 as the conservative smoke-mode setting for the initial combined test; use the new upstream native BotVision binary. This does not assert that upstream's newer volumetric mode is still slow. Mode 0 can be evaluated separately after the combined baseline is stable.

All four maintained projects target .NET 10 / CounterStrikeSharp API 1.0.376. Their assembly names remain unchanged so they replace the original plugin directories. No separate RayTrace API library is needed.

## Preserved upstream features

The new BotState, BotRandomizer, BotBuy, managed bridges, RoundDamageRecap, native plugins, new panel, profile VPKs and Rush configurations come from the original v1.4.5 package. The untouched feature-plugin DLLs and Rush cfg files were checked byte-for-byte against the download. This preserves their new content and behavior paths; gameplay still needs to verify the combined result.

The explicit `bot_aim body` priority order and the body/trunk choices for Head/Mixed modes match upstream 2.1.5. Upstream's removal of the old BotAI outer/inner FOV bypasses remains intact. Disabling global CT bomb information does not remove T-only bombsite knowledge or the separate new T defense/radio code. The current game's stock gameinfo and the new pack's stock gameinfo have no line differences after managed mounts/whitespace are normalized.

## Validation before game launch

- Four Release builds: zero warnings/errors.
- 19 smoke policy checks and 11 grenade-replay policy checks passed.
- All **38 active Windows BotAI patch sites** had unique signatures and matching original instruction bytes in the installed server.dll.
- Aim field instructions, smoke density function and three grenade factories audited against the game binary.
- Upstream body/trunk arrays and all pre-existing BotAI patch definitions preserved.
- A disposable 568-file upgrade from the old stable payload passed, including version guard, file hashes, API 376/new panel, old RayTrace parking, gameinfo/profile templates and full rollback to the old stable hashes.

These are build, static and installation checks. The user accepted the combined v1.4.5 local Windows playtest before release packaging. Remaining previously noted inventory/spend-cap and bomb-search/defuse regression limits still apply.

## Build and next playtest

Build each named project with `dotnet build -c Release`; NuGet restores CounterStrikeSharp.API 1.0.376. The pure tests can be run with `dotnet run --project tests/SmokeAimTests/SmokeAimTests.csproj -c Release` and the corresponding ReplaySafetyTests project.

For the initial Windows playtest, use the installed `Start-CompatiblePanel.cmd` (now opens Panel v1.4.5), start a normal Dust II bot match and play 4–5 rounds with combat and smoke. Check sensible grenade launches, blind fire instead of precise smoke tracking, varied cosmetics and smooth round starts. Rush, radio commands, T bomb defense and clear/smoke bomb defusing need focused follow-up checks.

The public installer, restore script and build tools live in packaging/. The full downloaded package contains the separately licensed upstream panel and is not a public redistribution asset. The earlier GitHub publication ZIP targets v1.4.4; this source snapshot and its public update now target v1.4.5.
