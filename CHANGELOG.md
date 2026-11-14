# Changelog

## v1.4.9-fairplay.1 - upstream v1.4.9 integration (stable)

Rebase the accepted Fairplay improvements onto original **CS2-Bot-Improver v1.4.9**, for Windows CS2 **2000943 / 1.41.9.7**.

- Use official **CounterStrikeSharp 380**, updated Metamod, the latest panel and BotHider's demotracer/unload compatibility fixes. The former local CSS 379 network-message adapter is retired.
- Retain only the BotController voice-message adapter: the original controller DLL and pinned source are unchanged and still need its descriptor lookup/allocation/release correction.
- Include the new upstream retake behavior trees and reload behavior. Seven-entry profile VPKs preserve those files while keeping humanized Medium/High reactions; the same policy is applied to retake config (0.35 / 0.26 seconds). Low stays upstream.
- Build release-pinned BotBuy 1.0.16 and BotRandomizer 1.3.5 sources/catalog with SDK 380. The original archive contains older builds; this update uses the source revisions pinned by the v1.4.9 tag.
- Preserve real grenade inventory and throw limits, native flash blindness, smoke aim fairness, approximate hearing, spacing, protected native reaction timers and checked stuck recovery. The unsafe bomb-search replacement stays disabled because its expected bytes/target still do not fit this actual Windows binary.

**Install the original v1.4.9 Windows pack with bundled CSS 380 first**, even when upgrading from a v1.4.8 Fairplay installation. Then extract the Fairplay Update ZIP outside the game, close CS2/panel and run Install.cmd. Start with Start-CompatiblePanel.cmd. The updater checks exact dependencies and game binaries, backs up changes and supports reinstall/restore.

The local v1.4.9 integration received positive gameplay feedback. All eight public DLLs are rebuilt and automatically checked; the public build's disabled diagnostic defaults have no separate live acceptance. Longer High/Hard feedback applies to the preceding Fairplay base, not an equivalent extended v1.4.9 test. Navigation stalls and unrelated crashes can remain; no Linux, dedicated/public-server, every-map or second-PC validation is claimed.

Detailed diagnostics remain off by default and bounded. Native/shared/runtime binaries, the original panel, VPK assets and personal logs/dumps are not redistributed. The update transforms the pinned BotController DLL locally and generates humanized profiles from the separately installed original files. Do not bypass guards after future game updates.


## v1.4.8-fairplay.3 - CS2 1.41.9.7 compatibility (stable)

Stable promotion on 2026-11-14. Restore compatibility with Windows CS2 ClientVersion **2000943 / patch 1.41.9.7**. This release includes the earlier Fairplay changes and two additions:

- Adapt CounterStrikeSharp 379 and BotController to the updated native network-message descriptors. This addresses the observed startup access violation in user-message handling and also updates message lookup, allocation and voice-message release. The installer transforms exact, pinned original DLLs locally, verifies the resulting hashes and backs them up. Native DLLs are not redistributed.
- Remove the unchecked periodic backwards push from stuck recovery. A stationary bot may instead take a short step toward its goal when standing clearance and ground support have been checked; bounded detour/crouch-jump recovery remains available.

The five version-gated maintained plugins now target 2000943. The previous wrong-target bomb-search call stays disabled. Humanized Medium/High reactions, real grenade inventory, fair flash/smoke handling, sound awareness, spacing and 8v8 defaults remain. Detailed diagnostics are off by default and bounded.

**Install:** obtain the original v1.4.8 Windows pack with its bundled CounterStrikeSharp 379, then extract the Fairplay Update ZIP outside the game, close CS2/panel and run Install.cmd. Existing v1.4.8 Fairplay installations can apply this update directly if their dependencies match; older v1.4.5 bases must be upgraded first. Start with the installed Start-CompatiblePanel.cmd.

Extended local Windows playtesting has now been reported successful, including the High (Hard) difficulty profile. This version is promoted to a stable release for the pinned local Windows setup. Gameplay binaries and behavior are unchanged from the November 12 preview. The clean public diagnostic defaults have automated validation; this does not establish separate live testing of that public build, every map, another PC, Linux or dedicated/public servers.

Supported target remains Windows x64, CS2 2000943 / 1.41.9.7, original v1.4.8 and CSS 379 with exact pinned dependencies. Future game updates require compatibility review. Detailed logging remains off by default. Existing users of the accepted November 12 preview do not need to reinstall for gameplay changes.

## v1.4.8-fairplay.2 - bomb-search crash hotfix (2026-11-11, preview)

Disable an unsafe native bomb-search call replacement. On tested Windows build 2000942 its legacy relative displacement targets an unrelated function, matching the failure function and return location in repeated Train/Inferno server.dll crashes shortly after round 11 starts. Native bombsite search is retained; no new native hook/address is installed.

BotAI is rebuilt as 1.8.14-fairplay.2. The other five maintained plugin binaries and their behavior are unchanged from the previous preview. Medium reaction fairness, grenade/flash/smoke handling and guarded recovery remain. A regression test excludes the faulty replacement and all previous unfair bypasses.

This is a targeted correction with strong crash evidence, pending repeated gameplay validation. It is not a general stability or navigation fix and does not resolve the distinct earlier CSS fast-fail. Upgrade from fairplay.1 is recommended; the original v1.4.8/CSS 379/game build requirements remain unchanged.


## v1.4.8-fairplay.1 - 2026-11-11 (preview)

- Rebased maintained components on original v1.4.8 and SDK 379. Preserve new upstream implementations and use typed Schema fields. Retire old CSS 376 and binary BotState adapters.
- Preserve fair smoke aiming, native flash blindness, real grenade purchases/pickups/consumption, plausible throw checks, approximate sound responses, smoke movement and teammate spacing.
- Restore native notice and fire/surprise timing. Remove forced attack-on-aim and permanent combat readiness overrides; exclude fire-rate-check and always-notice bypass patches. Keep humanized Medium/High presets and 16-player defaults.
- Add clearance/landing-checked crouch-jump and walking recovery, separate walking/jump lease limits, useful-progress direction retention, ineffective-direction rejection, close raised waypoint handling and native ladder handoff. These remain bounded recovery attempts, not a replacement navigation mesh.
- Add opt-in read-only last-survivor snapshots, including actual movement and ladder state, to catch moving loops missed by stationary detection.
- Four diagnostic modules are disabled by default and bounded to 8 MiB detail plus 1 MiB own errors altogether. Existing private log files are neither copied nor deleted.
- Preview limitations: remaining window/ladder/obstacle stalls; unresolved observed Train access violation; no Linux/public-server/second-PC certification. See docs/KNOWN-ISSUES.md.

## Earlier v1.4.5 Fairplay releases

Earlier releases established fair smoke/flash behavior, real grenade inventory and network notification fixes, humanized profile values, 8v8 defaults, and bounded opt-in diagnostics. Their release assets remain separate and target a different original pack/runtime baseline.
