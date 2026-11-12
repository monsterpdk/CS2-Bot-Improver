# CS2 Bot Improver - Windows Fairplay Edition

**v1.4.8-fairplay.3 (preview), 2026-11-12.** Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.8](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.8), for local Windows bot matches.

The original pack supplies the panel, cosmetics, native modules and runtime. This fork adds fair smoke/flash handling, real grenade inventory, realistic scripted throws, approximate sound awareness, teammate spacing, humanized reactions and guarded stuck recovery. Upstream Rush, radio commands, FOV, post-plant behavior and cosmetics remain available through the separately installed original pack.

## Latest update

The CS2 1.41.9.7 update changed native network-message descriptors. The updater now adapts the pinned CounterStrikeSharp 379 and BotController DLLs locally, including lookup, creation and voice-message release. It addresses the observed startup access violation without disabling message dispatch. Original native DLLs are obtained from upstream; the corresponding byte transformations and evidence are included here. See [native compatibility](docs/NATIVE-COMPATIBILITY.md).

Stuck recovery no longer applies an unchecked periodic backwards push. After persistent stationary movement failure, a short advance toward the goal is considered only with checked standing clearance and ground support. This does not replace map navigation or guarantee escape from every obstacle.

## Install

1. Install the **original v1.4.8 Windows pack with its bundled CounterStrikeSharp 379**. Older v1.4.5 bases must first be upgraded. Existing v1.4.8 Fairplay users can apply this update directly when their pinned dependencies match.
2. Download **CS2-Bot-Improver-Fairplay-Update-v1.4.8-fairplay.3-2026-11-12.zip** from Releases and extract outside the game folder.
3. Close CS2 and the panel. Run **Install.cmd** and select **game\csgo**.
4. Start the installed **Start-CompatiblePanel.cmd**. Use Bot Mode for local matches. The default is **8v8 including human players**.

Supported target: Windows x64, **ClientVersion 2000943 / patch 1.41.9.7**, SDK **1.0.379** and exact audited dependencies. Original and already-adapted native DLLs are accepted; unknown binaries are rejected. Replaced files are backed up, active difficulty and current gameinfo/layer paths are preserved, and reinstall/restore is supported. Do not bypass guards after a future game update. See [installation](docs/INSTALLATION.md).

## Fairplay behavior

- Medium/High profile reaction times are humanized. Selected plugins no longer erase native perception/fire/surprise delays. Profile values are not a guaranteed minimum sight-to-shot interval.
- Scripted grenades consume real purchased or picked-up inventory. Geometry, range, speed, heading, pitch and obstruction checks limit implausible throws.
- Native flash blindness is retained. Blind aim does not follow live target updates. Smoke concealment reduces precise tracking while allowing blind fire; hearing uses limited, delayed and approximate information.
- Recovery checks player-width clearance and ground support, measures progress, avoids repeating failed directions, handles close raised waypoints and yields to native ladder movement. Input leases are bounded and only owned controls are cancelled.
- The wrong-target bomb-search call replacement remains disabled, retaining native bombsite search.

## Logging

Detailed diagnostics are **off by default**. Status counters remain available. Temporarily enable `bot_nades_diagnostics on`, `bot_flash_diagnostics on`, `bot_stuck_diagnostics on` or `bot_crouch_diagnostics on`; disable them afterward. The observer does not sample entities while disabled. Four maintenance loggers together retain at most **8 MiB detail + 1 MiB own errors**, with fixed rotating filenames. Valve/Windows/CSS/upstream logs have separate policies. No personal logs or dumps are included. See [diagnostics](docs/DIAGNOSTICS.md).

## Preview status

The private trial has positive local feedback. Automated validation covers native instruction fixtures, managed policies and transactional install/restore; the clean public package has no separate live gameplay acceptance. Bots may still stall at some windows, ladders or obstacles. Unrelated crashes, chance hits and penetration remain possible. The current smoke-bomb search behavior is unchanged. Linux, dedicated/public servers and another computer have not received equivalent validation.

Grenade choices depend on stock, money, useful map lineups and safety checks. Some maps have no decoy lineups. Bot Mode uses `-insecure`; switch to Online mode for ordinary online play.

See [changes](CHANGELOG.md), [building](docs/BUILDING.md), [profiles](docs/REACTION-PROFILES.md), [behavior](docs/FAIR-AWARENESS.md), [known issues](docs/KNOWN-ISSUES.md) and [attribution](NOTICE.md). Game/runtime/native/shared DLLs, the original panel and VPK assets are not redistributed. This is not an official Valve or upstream release.
