# Port to CS2 ClientVersion 2000942

See compatibility/AUDIT.md and the settings/reports in compatibility/ for the pinned Windows build. The prior sound/smoke/spacing and reaction-profile fixes are retained. This release adds real grenade inventory, native flash fairness and an initially tested BotState recovery repair. The public sources additionally disable detailed logs by default and bound their storage.

The CSS 376 engine adapter still updates three instruction displacements to the audited authentication, SteamID and fake-client convar slots (75,70,77). Steam identities/authentication are preserved. The separate BotState adapter starts from the author's exact v1.4.5 DLL (or supported locally adapted variants), preserves native blindness and repairs one-shot recovery/per-tick jump rearming. No replacement CSS/BotState dependency DLL or game binary is redistributed; output hashes and all transformation bytes are supplied.

Behavioral baseline passed local playtests; public logging defaults are build/policy/install tested without a separate live playtest. The initial cause of the recorded simultaneous Mirage stall is not established. Windows-only validation does not imply support for upstream Linux/dedicated-server use. Future game/runtime builds must be re-audited. No private event logs, dumps, backups or local paths are included in public source.
