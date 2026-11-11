# CS2 Bot Improver - Windows Fairplay Edition

**v1.4.8-fairplay.1 (preview), 2026-11-11.** Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.8](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.8), for local Windows bot matches.

The original pack supplies the bot features, panel, cosmetics, native modules and runtime. This fork adds fair smoke/flash handling, real grenade inventory, realistic scripted throws, approximate sound awareness, teammate spacing, humanized reactions and guarded stuck recovery. Upstream Rush, radio commands, FOV, post-plant behavior and updated cosmetics remain available through the separately installed original pack.

## Install

1. Install the **original v1.4.8 Windows pack with its bundled CounterStrikeSharp 379**. An older v1.4.5 installation must first be upgraded to that baseline; this update does not contain the original runtime or panel.
2. Download **CS2-Bot-Improver-Fairplay-Update-v1.4.8-2026-11-11.zip** from Releases and extract outside the game folder.
3. Close CS2 and the panel. Run **Install.cmd** and enter your **game\csgo** folder.
4. Start the installed **Start-CompatiblePanel.cmd**. Use Bot Mode for local matches. The default is **8v8 including human players**.

Supported target: Windows x64, **ClientVersion 2000942 / patch 1.41.9.6**, SDK **1.0.379** and exact audited dependencies. The installer rejects mismatches, backs up replaced files, preserves current gameinfo/layer paths and active difficulty selection, and supports reinstall/restore. Do not bypass version/hash guards after a game update. See [installation](docs/INSTALLATION.md).

## What changed

- Medium/High profile reaction times are humanized, and native perception/fire/surprise delays are no longer erased by BotState or bypassed by selected BotAI patches. Medium playability has positive local feedback. Profile values are not a guaranteed minimum sight-to-shot interval.
- Scripted grenades consume real purchased or picked-up inventory. Release geometry, speed, range, heading, pitch and obstruction checks limit implausible throws. Native grenade counts and ownership remain relevant.
- Bots retain native flash blindness; blind aim no longer follows live target updates. Smoke concealment reduces precise tracking while allowing blind fire. Sound reactions use limited, delayed and approximate information.
- Recovery checks player-width clearance and landing support before crouch-jumps or short detours, measures actual progress, avoids repeating ineffective directions, handles close raised waypoints, and yields to native ladder movement. It uses bounded input leases and cancels only its own controls.
- Updated to the v1.4.8 component sources and typed SDK 379 fields. The old CSS 376/BotState byte adapters are not used or shipped.

## Logging

Detailed diagnostics are **off by default**, with status counters available. Temporarily enable `bot_nades_diagnostics on`, `bot_flash_diagnostics on`, `bot_stuck_diagnostics on`, or `bot_crouch_diagnostics on`; turn them off afterward. The observer does not sample entities while disabled. Four maintenance loggers together keep at most **8 MiB detail + 1 MiB own errors**, using fixed rotating filenames. This does not bound Valve's, Windows', CSS's or other upstream logs. No personal logs/dumps are included. See [diagnostics](docs/DIAGNOSTICS.md).

## Preview status and known limitations

This is a **preview**, not a claim that navigation or stability is solved. Local play supports the reaction, grenade, smoke and flash changes and improved desk recovery. Bots can still stall at some windows, ladders and obstacles; overlapping bots can complicate recovery. An observed Train crash was a server.dll access violation with an invalid object reference; its root cause is unproven. No crash fix is claimed. High, Linux, dedicated/public servers and another PC have not received equivalent validation. Public logging defaults/version labels have automated checks but no separate live gameplay acceptance.

Grenade choices depend on inventory, money, useful recorded map lineups and safety. Some maps have no decoy lineups. Blind shots/chance hits and penetration remain possible. Bot Mode uses `-insecure`; switch to Online mode for ordinary online play.

See [changes](CHANGELOG.md), [build instructions](docs/BUILDING.md), [reaction profiles](docs/REACTION-PROFILES.md), [behavior](docs/FAIR-AWARENESS.md), [known issues](docs/KNOWN-ISSUES.md) and [attribution](NOTICE.md). Game/runtime/native/shared DLLs, the original panel and VPK assets are obtained from their authors, not redistributed in this release. Not affiliated with Valve or endorsed by upstream maintainers.
