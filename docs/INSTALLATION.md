# Windows installation — v1.4.5 Fairplay

## Prerequisites

1. Install CS2 through Steam. Find its `game/csgo` folder (not the Steam installation root). This release requires `ClientVersion=2000924` in `steam.inf`.
2. Close CS2 and the panel. Use a clean mod installation; keep a separate backup if upgrading from our old v1.4.4 package. Obtain the original [v1.4.5 Windows asset](https://github.com/ed0ard/CS2-Bot-Improver/releases/download/v1.4.5/CS2BotImprover.zip) from its author. Copy its `addons`, `cfg`, `overrides`, panel EXE, ServerConfig and mapcycle files to `game/csgo`. Preserve the game's current stock `gameinfo.gi`; do not copy old `gameinfo.gi` or `backup` templates. Do not launch the original panel yet. The installer will create valid templates from the target gameinfo.
3. Keep the original pack's bundled CounterStrikeSharp 1.0.376 runtime. No separate CSS 373 install or RayTrace API is required. For a fresh mod installation, skip the pack's `addons/metamod/RayTrace.vdf` placeholder; the updater also parks it if present.

If a previous bot pack replaced the stock gameinfo, restore the proper Online template or validate the game's files in Steam before installing. Steam validation does not remove unrelated old addon directories. This updater deliberately rejects extra managed plugins; it is not an automatic v1.4.4-to-v1.4.5 runtime migration.

## Apply the update

1. Extract `CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-10-04.zip` outside the game folder.
2. Run `Install.cmd` and enter the full `game/csgo` path, or run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -Csgo "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo"
```

3. The installer verifies payload, game version, pinned native/core/shared/feature dependencies, gameinfo layers and plugin list before game writes. It backs up overwritten files, merges only `AutoUpdateEnabled=false` and `FollowCS2ServerGuidelines=false` into core.json, copies the maintained files and creates Online/Bot templates. No old gamedata overrides are applied. Failed writes trigger rollback.
4. Start `game/csgo/Start-CompatiblePanel.cmd`, choose Bot Mode, and start a local bot match. The wrapper opens Panel v1.4.5.
5. Check team selection, several round starts, grenades, smoke blind fire and cosmetics. The tested smoke setting is `bv_smoke_mode 1`. Profiles remain selectable in the original panel; the public update does not replace profile VPKs.

The supplied cfg files are bot-match settings, including the new Rush cfgs; they replace corresponding cfgs and can be adjusted with the panel. They are not a public-server rules preset. Use Online mode through the wrapper before ordinary online play. Direct modded Steam launch needs `-insecure`. If the original panel needs WebView2 or the native runtime needs Visual C++ x64 libraries, obtain them from Microsoft. Players do not need a .NET SDK.

## Verify and restore

`Install.ps1 -VerifyOnly` verifies the extracted update without installing. SHA-256 sidecars identify the ZIPs:

```powershell
Get-FileHash .\CS2-Bot-Improver-Fairplay-Update-v1.4.5-2026-10-04.zip -Algorithm SHA256
```

Backups are under `game/csgo/_botimprover_backups/v1.4.5-fairplay-<timestamp>/`. With CS2/panel closed, restore using:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\_botimprover_backups\v1.4.5-fairplay-<timestamp>"
```

Restore verifies saved files before writes and reverses only this update transaction. It does not undo the separately installed original pack. Dependency failures name the incompatible file; use the pinned v1.4.5 pack rather than bypassing checks or mixing arbitrary DLLs.
