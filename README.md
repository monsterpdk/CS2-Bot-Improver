# CS2 Bot Improver ? Windows Fairplay Edition

Current release: **v1.4.5-fairplay.4**, 2026-11-08. Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.5](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.5) for local Windows bot matches.

Fairer smoke aiming, real grenade inventory, native flash blindness, approximate sound awareness, smoke movement assistance and teammate spacing. Medium/High reaction times remain humanized; the default is **8v8 including the human player**. Upstream Rush support, radio commands, FOV alignment, post-plant defense, cosmetics and panel features are retained.

Tested game: **Windows x64, CS2 ClientVersion 2000942 / patch 1.41.9.6, CounterStrikeSharp 376**. Exact version, game binary and dependency checks are enforced.

## Install

Install the author's original v1.4.5 Windows pack first. Download this fork's **CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-11-08.zip** from Releases, extract it outside the game folder, close CS2 and the panel, then run **Install.cmd** and select **game\csgo**. Start the installed **Start-CompatiblePanel.cmd** and choose Bot Mode. Existing Fairplay installations can use this update when dependency checks pass. GitHub's automatic Source code archives are developer downloads.

The updater includes five maintained managed plugins and corresponding source, map data, match configs and independent compatibility tools. It derives the CSS engine-interface and BotState flash/recovery adaptations from the user's locally installed original DLLs, preserves current gameinfo/layer paths, and generates the reaction profiles locally. The original panel, runtime/native/shared DLLs and VPK assets must be obtained from their authors; the complete personal-use bundle is not a public release asset.

## Logging and troubleshooting

Detailed logging is **disabled by default**. Normal status counters remain available without detailed logs. For a temporary investigation use `bot_nades_diagnostics on`, `bot_flash_diagnostics on`, or `bot_stuck_diagnostics on`; use `off` afterward. The stationary observer does not sample entities while disabled. Commands apply to the current plugin session; explicit diagnostics.json settings can enable later sessions. Our three diagnostic loggers together are bounded to **6 MiB detail + 768 KiB errors**. This does not limit the game's or other plugins' own logs.

See [diagnostics and storage](docs/DIAGNOSTICS.md), [installation and restore](docs/INSTALLATION.md), [changes](CHANGELOG.md), [behavior](docs/FAIR-AWARENESS.md), [reaction profiles](docs/REACTION-PROFILES.md), [build instructions](docs/BUILDING.md), and [attribution](NOTICE.md).

## Validation and limits

Inventory, flash fairness and the network notification correction passed local gameplay tests. Stuck recovery has an initial positive test; it remains a preliminary improvement, and the origin of the recorded simultaneous Mirage stall is not established. Automated checks cover source builds, policies, bounded/off-by-default diagnostics, exact native assumptions and install/rollback/restore. The public logging defaults have not had a separate live playtest.

- Windows local matches only. Linux, public/dedicated servers, every map/mode and another PC are not validated.
- Smoke/flash safeguards reduce unfair tracking; blind fire, chance hits and bullet penetration remain possible.
- Grenade choices depend on owned inventory, money, map lineups, opportunities and safety checks. Full human throw animations are not simulated. Some maps have no decoy lineups.
- Native navigation can still stall. Recovery retries are not a complete path planner or a proven cure for every team-wide stall.
- Bot Mode uses `-insecure`; use the wrapper's Online mode for ordinary online play.
- A future Steam update needs another compatibility review. Do not disable the installer's guards.

Preserve all applicable licenses and authors' notices. Not affiliated with Valve or endorsed by upstream maintainers.
