# Installation and restore

Use original v1.4.8 Windows with CSS 379 on ClientVersion 2000942. Older Fairplay releases based on v1.4.5 are not an alternative runtime baseline. First install the new original pack, then apply this update. Existing humanized Low/Medium/High files are accepted and the active selection is preserved; unknown custom profiles are rejected before writing.

Extract the release ZIP outside game\csgo. With game/panel closed, run Install.cmd and enter the folder containing gameinfo.gi and steam.inf. Exact game binary and upstream dependency hashes are checked. Extra unknown CSS plugin directories are rejected. Keep your existing installation backed up independently when switching upstream versions.

The updater includes six maintained managed plugins, data/configs and independent tools. It merges target gameinfo without replacing Steam's layered paths, creates Bot/Online templates, sets fill quota 16, and disables automatic runtime overwrites for the custom pack. Obsolete RayTrace.vdf is parked in the backup when present. Original runtime/shared/native modules remain unchanged.

All replaced files are saved to game\csgo\_botimprover_backups\v1.4.8-fairplay.1-TIMESTAMP. The installer rolls back copy failures. To restore this update with game and panel closed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore.ps1 -Backup "FULL_BACKUP_FOLDER"
```

Use the Restore.ps1 from this extracted update. This restores the state just before this update; it does not roll back Steam or an earlier manual upstream upgrade. Reinstalling is supported. Future game updates need review; do not edit hash guards to force compatibility. No second-PC gameplay claim is made.
