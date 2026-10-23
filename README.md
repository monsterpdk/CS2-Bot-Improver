# CS2 Bot Improver — Windows Fairplay Edition

Current release: **v1.4.5-fairplay.3**, 2026-10-23. Independent maintenance of [ed0ard/CS2-Bot-Improver v1.4.5](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.5) for local Windows bot matches.

This edition adds fairer smoke aiming and human-scale grenade safeguards, delayed reactions to audible enemies, assistance for idle bots in smoke, and teammate spacing. It retains upstream's Rush support, FOV alignment, radio commands, post-plant defense, cosmetics and panel features. Medium/High reaction profiles remain humanized; default population is **8v8 including the human player**.

Tested with **Windows x64, CS2 ClientVersion 2000935 / patch 1.41.9.2, CounterStrikeSharp 376**. Exact game binary/dependency checks prevent applying these native assumptions to a different build.

## Download and install

Install the author's original v1.4.5 Windows pack first. From this fork's Releases download **CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-10-23.zip**, extract it outside the game folder, close CS2 and the panel, then run **Install.cmd** and select the target **game\csgo** folder. Launch the installed **Start-CompatiblePanel.cmd** and choose Bot Mode. Existing fairplay.1/.2 installations can use the same update if the required dependencies are unchanged. GitHub's Source code downloads are for developers.

The updater includes our four maintained managed plugins, map data, match cfgs, independent compatibility tools and corresponding source. It generates reaction profiles locally, preserves the target game's gameinfo/layer paths, merges settings, and applies a three-byte CSS 376 engine-interface adapter to the author's locally installed DLL. It also updates the BotVision smoke signature. The panel, runtimes, native/shared binaries and VPK assets are obtained from their authors. The full personal-use bundle is **not** a public release asset.

See [installation and restore](docs/INSTALLATION.md), [changes](CHANGELOG.md), [behavior and limits](docs/FAIR-AWARENESS.md), [reaction profiles](docs/REACTION-PROFILES.md), [build instructions](docs/BUILDING.md), and [attribution](NOTICE.md).

## Validation and limits

The user accepted the combined local playtest as stable. The last test recorded 44 detonated smokes, 3 HE grenades, 10 flashes and 8 created Molotovs. Source builds, policy/startup/contract checks, native signature audits and disposable install/rollback/restore checks accompany the release. The behavior uses approximate, expiring sound memories rather than a hidden-enemy position feed; it does not implement human-equivalent hearing or full autonomous path planning.

- Windows local matches only; Linux, dedicated/public servers, every map/mode and a second PC are not validated.
- Blind fire can still hit through smoke; visibility safeguards do not remove bullet penetration or every native AI advantage.
- Grenade usage depends on map lineups, opportunities, budget and limits. The system does not fully simulate inventory or throw animations.
- Native navigation can still stall in unusual geometry. Bomb recollection and nearby audible bomb hints are bounded assistance, not a complete search simulation.
- Bot Mode uses `-insecure`; switch through the wrapper to Online mode before ordinary online play.
- Installers back up their transaction and refuse unknown extra managed plugins/dependencies. A future Steam update needs another compatibility review.

Preserve all applicable licenses and authors' notices. This project is not affiliated with Valve or endorsed by upstream maintainers.
