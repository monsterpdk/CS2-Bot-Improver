# Diagnostics and disk use

All three maintained detailed loggers default to off. Without an error they create no diagnostic files. Normal engine/framework startup and error reporting remain separate.

| Module | Current-session switch | Status |
| --- | --- | --- |
| Grenade inventory | `bot_nades_diagnostics on` / `off` | `bot_nades_inventory`, `bot_nades_status` |
| Flash fairness | `bot_flash_diagnostics on` / `off` | `bot_flash_status` |
| Stationary observer | `bot_stuck_diagnostics on` / `off` | `bot_stuck_status` |

The observer's `active=False` is expected by default. After enabling it, `live=True` becomes expected after round freeze ends. `bot_stuck_mark <name-or-slot>` records an immediate snapshot while observation is active. A stationary candidate is not proof of a bug: legitimate guarding may qualify.

Each module reads `diagnostics.json` in its own plugin folder, with `{"Enabled":false}` supplied. Set it to true only to retain detailed logging over future game sessions. Runtime commands do not rewrite the file. Installing this release resets the supplied diagnostics.json defaults to false; the install backup preserves previous settings. Diagnostic logging does not enable cheats or alter grenade/smoke/flash behavior.

The fixed files live under each plugin's logs directory: fairplay-inventory.jsonl, fairplay-flash.jsonl and fairplay-stuck.jsonl. A .1 archive is the only rotated detail file. Each is at most 1 MiB, making 2 MiB per module and 6 MiB combined. Each module also has two error files of at most 128 KiB, adding at most 768 KiB combined. Rotation applies across sessions and maps and only touches those fixed filenames. Old timestamp-based logs from local trial builds are not deleted automatically; these are not part of the release download.

Game/CounterStrikeSharp/upstream-plugin logs are outside this limit. No screenshots, personal logs, crash dumps, backups or locally recorded player sessions are bundled. If you share an opt-in diagnostic file for support, review any player names it contains first.
