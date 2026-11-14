# CS2 Bot Improver - Windows Fairplay Edition

**v1.4.9-fairplay.1 (stable), 2026-11-14.** Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.9](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.9), for local Windows bot matches.

The original pack supplies the panel, cosmetics, native modules and runtime. This fork adds fair smoke/flash handling, real grenade inventory, realistic scripted throws, approximate sound awareness, teammate spacing, humanized reactions and guarded stuck recovery. Upstream Rush, radio commands, FOV, post-plant behavior and cosmetics remain available through the separately installed original pack.

## Latest update

The release follows [upstream v1.4.9](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.9): official CSS 380 replaces our CSS 379 adapter; updated Metamod, panel and BotHider fix compatibility including demotracer. New retake trees/reload behavior are retained, with humanized Medium/High reactions layered onto the seven-entry profiles.

The unchanged BotController still requires its voice-message descriptor adapter. It is applied locally to an exact upstream DLL with verified output hashes and backups; native binaries are not bundled. Eight managed modules are built from the pinned/maintained sources, including BotBuy 1.0.16 and BotRandomizer 1.3.5. See [native compatibility](docs/NATIVE-COMPATIBILITY.md).

## Install

1. Install the **original v1.4.9 Windows pack with its bundled CounterStrikeSharp 380**. Older v1.4.5/v1.4.8 bases must first be upgraded to original v1.4.9, including the runtime and panel.
2. Download **CS2-Bot-Improver-Fairplay-Update-v1.4.9-fairplay.1-2026-11-14.zip** from Releases and extract outside the game folder.
3. Close CS2 and the panel. Run **Install.cmd** and select **game\csgo**.
4. Start the installed **Start-CompatiblePanel.cmd**. Use Bot Mode for local matches. The default is **8v8 including human players**.

Supported target: Windows x64, **ClientVersion 2000943 / patch 1.41.9.7**, SDK **1.0.380** and exact audited dependencies. The original or exact already-adapted BotController DLL is accepted; unknown binaries are rejected. Replaced files are backed up, active difficulty and current gameinfo/layer paths are preserved, and reinstall/restore is supported. Do not bypass guards after a future game update. See [installation](docs/INSTALLATION.md).

## Fairplay behavior

- Medium/High profile reaction times are humanized. Selected plugins no longer erase native perception/fire/surprise delays. Profile values are not a guaranteed minimum sight-to-shot interval.
- Scripted grenades consume real purchased or picked-up inventory. Geometry, range, speed, heading, pitch and obstruction checks limit implausible throws.
- Native flash blindness is retained. Blind aim does not follow live target updates. Smoke concealment reduces precise tracking while allowing blind fire; hearing uses limited, delayed and approximate information.
- Recovery checks player-width clearance and ground support, measures progress, avoids repeating failed directions, handles close raised waypoints and yields to native ladder movement. Input leases are bounded and only owned controls are cancelled.
- The wrong-target bomb-search call replacement remains disabled, retaining native bombsite search.

## Logging

Detailed diagnostics are **off by default**. Status counters remain available. Temporarily enable `bot_nades_diagnostics on`, `bot_flash_diagnostics on`, `bot_stuck_diagnostics on` or `bot_crouch_diagnostics on`; disable them afterward. The observer does not sample entities while disabled. Four maintenance loggers together retain at most **8 MiB detail + 1 MiB own errors**, with fixed rotating filenames. Valve/Windows/CSS/upstream logs have separate policies. No personal logs or dumps are included. See [diagnostics](docs/DIAGNOSTICS.md).

## Stable release and validation scope

The local v1.4.9 integration was reported working correctly. Longer local play, including High/Hard, validated the preceding Fairplay base; no equivalent extended v1.4.9 session is claimed. The clean public build has automated validation and disabled diagnostic defaults, with no separate live acceptance. Some navigation stalls, chance hits and unrelated crashes remain possible. Linux, dedicated/public servers, another PC and every map are not validated. Smoke-bomb search behavior is unchanged by this rebase.

Grenade choices depend on stock, money, useful map lineups and safety checks. Some maps have no decoy lineups. Bot Mode uses `-insecure`; switch to Online mode for ordinary online play.

See [changes](CHANGELOG.md), [building](docs/BUILDING.md), [profiles](docs/REACTION-PROFILES.md), [behavior](docs/FAIR-AWARENESS.md), [known issues](docs/KNOWN-ISSUES.md) and [attribution](NOTICE.md). Game/runtime/native/shared DLLs, the original panel and VPK assets are not redistributed. This is not an official Valve or upstream release.
