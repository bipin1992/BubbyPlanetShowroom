# Bubby Planet Showroom Deployment Scripts

## 1) Build machine (your current system)

Run these commands from project root:

```powershell
# Self-contained single-file publish (recommended; outputs a single .exe)
.\scripts\publish-app.ps1

# Backup MySQL database
.\scripts\backup-db.ps1 -User root -Password "YOUR_PASSWORD"
```

Output:
- `publish-output\...\` folder
- `publish-output\...\.zip`
- `db-backup-showroom_db.sql`

Copy ZIP and SQL backup to target machine.

## One-Click Target Setup (Automatic)

If you want install + DB restore + app launch in one command, run on target machine:

```powershell
.\scripts\one-click-setup.ps1 `
  -AppFolder "D:\BubbyPlanetShowroom" `
  -BackupFile "D:\BubbyPlanetShowroom\db-backup-showroom_db.sql" `
  -MySqlUser root `
  -MySqlPassword "YOUR_PASSWORD" `
  -CreateDesktopShortcut
```

Notes:
- Script auto-runs as Administrator.
- It tries MySQL auto-install via `winget` if available.
- If `winget` is unavailable, pass installer file path:

```powershell
.\scripts\one-click-setup.ps1 `
  -AppFolder "D:\BubbyPlanetShowroom" `
  -BackupFile "D:\BubbyPlanetShowroom\db-backup-showroom_db.sql" `
  -MySqlInstallerPath "D:\Installers\mysql-installer-community-8.0.xx.x.msi" `
  -MySqlUser root `
  -MySqlPassword "YOUR_PASSWORD" `
  -CreateDesktopShortcut
```

## 2) Target machine setup

1. Install MySQL Server (8.x recommended).
2. Extract published ZIP to a folder, e.g. `D:\BubbyPlanetShowroom`.
3. Restore DB:

```powershell
.\scripts\restore-db.ps1 -User root -Password "YOUR_PASSWORD"
```

4. Run app + create shortcut:

```powershell
.\scripts\setup-target.ps1 -AppFolder "D:\BubbyPlanetShowroom" -CreateDesktopShortcut
```

## Notes

- Visual Studio is NOT required on target machine.
- If using default code connection string (`localhost`), MySQL must run on same target machine.
- If MySQL installed at custom path, pass `-MySqlBin "C:\path\to\mysql\bin"` in backup/restore scripts.
