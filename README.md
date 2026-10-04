# CS2 Bot Improver — Windows Fairplay Edition

A community maintenance update for [CS2-Bot-Improver v1.4.5](https://github.com/ed0ard/CS2-Bot-Improver/releases/tag/v1.4.5), adding safer grenade throws and fairer smoke aiming for local Windows bot matches. Independent project; not an official upstream or Valve release.

**Release: `1.4.5-fairplay.1` — 2026-10-04.** Tested with Windows x64, CS2 `ClientVersion 2000924` / patch `1.41.8.8`, and the original v1.4.5 Windows pack's bundled CounterStrikeSharp `1.0.376`. Future CS2 updates require another compatibility review.

## Our additions to v1.4.5

| Area | What this update changes |
| --- | --- |
| Grenades | Anchors launches to the throwing bot's current eye position; checks finite values, release offset, speed, range, heading, pitch and launch obstruction. Revalidates delayed spawns before applying the throw. |
| Smoke aiming | Concealed opponents' current positions do not drive the managed aim override. Bots use a previously observed point or a fixed blind-fire direction instead. |
| Blind fire | Favors open travel directions at eye height when no remembered point is available; limits downward/upward pitch for remembered points. |
| Smoke visibility | Corrects the Windows branch that otherwise treats two positions inside smoke as visible. Keeps `bv_smoke_mode 1` for this tested setup, using upstream's new native BotVision. |
| Bomb information | Disables three visibility/global-information bypasses that could give CT bots unrealistic bomb knowledge; keeps upstream's T defense code. This is not a complete human-like bomb-search simulation. |
| Startup and deployment | Regenerates panel gameinfo templates from the installed game's own file. Adds version/dependency/hash checks, backups, rollback and explicit restore. |

Detailed grenade/smoke diagnostic sampling is not enabled in this release. The four modified plugins are BotAI `1.8.12-fairplay.1`, BotAimImprover `2.1.5-fairplay.1`, NadeSystem `1.2.2-fairplay.1`, and BotVisionCompatibility `0.1.4-fairplay.1`.

## Upstream improvements retained

The v1.4.5 base supplies repaired functions, Rush behavior trees/configurations, human-aligned bot FOV, new sticker/music-kit content, post-plant T bomb defense, player radio commands, revised `bot_aim body` priorities, and its redesigned panel with the Rules menu. These are upstream features, credited to their authors. This update preserves the untouched feature DLLs and new aim priorities; it does not restore our old v1.4.4 compatibility patches or the removed FOV bypasses.

Upstream also advertises Linux support. **This maintenance update is validated for Windows local games only**; its additional smoke correction is Windows-specific.

## Download and install

Download **`CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-10-04.zip`** from this repository's Releases. GitHub's automatic Source code downloads are not installers.

This public asset is an **update**: obtain the original v1.4.5 Windows pack from its author first, then apply our update. The panel, runtimes, native/shared binaries and VPKs are not redistributed here. The original pack already contains the tested CSS 376 runtime; do not replace it with the old CSS 373 setup.

Follow [INSTALLATION.md](docs/INSTALLATION.md), then launch `game/csgo/Start-CompatiblePanel.cmd`. Use Bot Mode for private/local bot matches; the panel uses `-insecure`. Use the wrapper and Online mode before ordinary online play.

## Validation and limits

The merged v1.4.5 setup was accepted after the user's local Windows playtest. The earlier grenade/smoke behavior had also been tested in Dust II/Cache matches. Four Release builds, 19 smoke-policy checks, 11 grenade-policy checks, gameinfo tests, native signature/instruction audits and disposable installer/restore tests accompany this release. This does not certify every upstream mode or behavior in every situation.

- Future game builds, Linux, dedicated/public servers, every map and a second PC are not validated.
- Blind fire can still hit players through smoke. The guard limits targeted aiming, not bullet penetration or every native AI advantage.
- Grenade safeguards do not fully simulate inventory, throwing animations or legal per-round purchases. Spend-cap/inventory behavior still needs work; decoy use depends on available map data.
- CT bomb search/defuse in smoke, Rush, radio and T post-plant defense still need focused regression tests beyond the accepted combined playtest.
- Mode 1 is the tested setting, not a claim that the new upstream mode 0 still has the previous version's performance problem.
- The installer refuses unexpected extra managed plugins and mismatched pinned dependencies. It backs up its own transaction, not unrelated changes made beforehand.

See [CHANGELOG.md](CHANGELOG.md), [PORT-NOTES.md](PORT-NOTES.md), [NOTICE.md](NOTICE.md), and [build instructions](docs/BUILDING.md). Source is provided with the public binaries; preserve the applicable licenses and authors' notices.
