# Diagnostics and storage

All four diagnostics.json files default to {"Enabled":false}. Status counters remain available without detail logs:

| Module | Status | Temporary detailed logging |
| --- | --- | --- |
| NadeSystem | bot_nades_status / bot_nades_inventory | bot_nades_diagnostics on/off |
| BotAimImprover | bot_flash_status / bot_fair_awareness status | bot_flash_diagnostics on/off |
| BotState | bot_crouch_recovery | bot_crouch_diagnostics on/off |
| BotStuckDiagnostics | bot_stuck_status | bot_stuck_diagnostics on/off |

Commands apply to the current plugin session; set Enabled in that module's diagnostics.json for later sessions. The observer performs no entity sampling while disabled. Enabled observer context records include the last bot alive on each team every five seconds; this is not an automatic stuck classification. Recovery logs distinguish actual progress from lease completion.

Each logger uses a fixed detail filename and one archive, each <=1 MiB, plus an error file and archive, each <=128 KiB. Across four modules: <=8 MiB details and <=1 MiB own errors. Rotation persists across maps/restarts without timestamp file proliferation. Existing private trial logs are left alone and not counted in this new logger bound. Valve/Windows/CSS and other plugin logs have separate policies. No old logs are removed by installation.

For reports include release, map, bot name, rough time, whether it was last alive, and relevant status. Logs can contain player names/positions; inspect before sharing. Memory dumps can contain private process data and should not be uploaded publicly. No private logs, personal paths or dumps are shipped.
