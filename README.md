# CS2 Bot Improver - Windows Fairplay Edition

**v1.4.10-fairplay.1 (stable), 2026-11-25.** Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.10](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.10), for local Windows bot matches.

The original pack provides the panel, runtime, native modules and cosmetics. This fork adds fair smoke/flash handling, actual grenade inventory and realistic throws, approximate hearing/spacing, humanized reactions and checked stuck recovery. Upstream game-mode behavior and improvements are retained.

## Latest update

Official **CSS 381, BotHider and BotController** supersede all our local native DLL adapters, including the voice-message fix. They are used unmodified. BotAI now uses official 1.9.1's JSON/overlap/memory-protection implementation. Its corrected planted-site getter is enabled; the old crash-safety exclusion is removed. Seven deliberate notice/fire/knowledge fairness exclusions remain. Only one data-only weapon-removal signature narrowing is still needed.

The new original mode cfgs and source-pinned catalog are included: 773 new stickers and 8 music kits. The accepted local rebase completed one full Inferno match, about 15 rounds.

## Install

1. Install **original v1.4.10 Windows with bundled CSS 381** first. Existing v1.4.9 Fairplay users must upgrade that original base, including native/API/shared/implementation files. The accepted local v1.4.10 integration already has them.
2. Extract **CS2-Bot-Improver-Fairplay-Update-v1.4.10-fairplay.1-2026-11-25.zip** outside the game.
3. Close CS2/panel, run **Install.cmd** and select **game\csgo**.
4. Start the installed **Start-CompatiblePanel.cmd**, choose Bot Mode. Default population is 8v8 including humans.

Target: **Windows x64, ClientVersion 2000950 / 1.42.0.1, API 381** with exact dependencies. Native DLLs must match original v1.4.10; this release neither edits nor accepts the retired old adapters. A narrow gamedata text update accepts the original or exact corrected known data. Backups, settings/difficulty preservation, reinstall and restore are supported. See [installation](docs/INSTALLATION.md).

## Fairplay behavior

- Medium/High reaction profiles are humanized; our behavior no longer erases native perception/fire/surprise timers. Profile reaction values are not a guaranteed minimum notice-to-shot interval.
- Scripted grenades consume actually owned purchased/picked-up inventory, with release geometry/range/speed/heading/pitch/obstruction checks.
- Native flash blindness remains active; blind aim does not track live targets. Smoke concealment permits blind fire toward limited remembered/approximate information.
- Sound/movement/spacing assistance is bounded. Recovery checks clearance/ground, measures progress, avoids repeated failed directions, yields to native ladders and cancels only its own bounded input leases.
- Official corrected T bomb-site getter is restored. Five CT/global knowledge shortcuts remain excluded; this is not a complete human bomb-search model.

## Diagnostics and validation

Detailed diagnostics are **off by default** and bounded. Counters remain available. Four loggers retain at most 8 MiB detail plus 1 MiB own errors when enabled; other software's logs have separate policies. No personal logs/dumps are included. See [diagnostics](docs/DIAGNOSTICS.md).

The private integration completed one full de_inferno match (about 15 rounds). Public managed modules have automated validation and disabled logging defaults, without a separate live acceptance. Every map/mode, another PC, Linux and dedicated/public servers are not validated. Navigation stalls/chance hits/unrelated crashes can remain. See [test results](docs/TEST-RESULTS.md) and [known issues](docs/KNOWN-ISSUES.md).

Cobblestone (`de_cbble`) ships with navigation, but this release does not add recorded grenade lineups for it. Grenade use also depends on stock, money, lineups and safety checks. Bot Mode uses -insecure; use Online mode for ordinary online play.

See [changes](CHANGELOG.md), [building](docs/BUILDING.md), [profiles](docs/REACTION-PROFILES.md), [behavior](docs/FAIR-AWARENESS.md), [compatibility](docs/NATIVE-COMPATIBILITY.md) and [attribution](NOTICE.md). Native/game/runtime/shared DLLs, the original panel and VPK assets are obtained separately. This is not an official Valve/upstream release.
