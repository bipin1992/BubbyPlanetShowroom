param(
    [string]$AppFolder = ".",
    [string]$ExeName = "BubbyPlanetShowroom.exe",
    [string]$BackupFile = "db-backup-showroom_db.sql",
    [string]$Database = "showroom_db",
    [string]$MySqlUser = "root",
    [string]$MySqlPassword = "",
    [string]$MySqlBin = "C:\Program Files\MySQL\MySQL Server 8.0\bin",
    [string]$MySqlInstallerPath = "",
    [switch]$CreateDesktopShortcut,
    [switch]$SkipMySqlInstall
)

$ErrorActionPreference = "Stop"

function Ensure-Admin {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "Relaunching script as Administrator..." -ForegroundColor Yellow
        $argList = @()
        foreach ($k in $PSBoundParameters.Keys) {
            $v = $PSBoundParameters[$k]
            if ($v -is [switch]) {
                if ($v.IsPresent) { $argList += "-$k" }
            } else {
                $argList += "-$k"
                $argList += ('"{0}"' -f $v)
            }
        }

        Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`" $($argList -join ' ')"
        exit
    }
}

function Resolve-PathSafe([string]$p, [string]$base) {
    if ([string]::IsNullOrWhiteSpace($p)) { return $p }
    if ([System.IO.Path]::IsPathRooted($p)) { return $p }
    return Join-Path $base $p
}

function Ensure-MySqlPresent {
    param([string]$BinPath, [string]$InstallerPath, [switch]$SkipInstall)

    $mysqlExe = Join-Path $BinPath "mysql.exe"
    if (Test-Path $mysqlExe) {
        Write-Host "MySQL binaries found at: $BinPath" -ForegroundColor Green
        return
    }

    if ($SkipInstall) {
        throw "MySQL not found at '$BinPath' and -SkipMySqlInstall was used."
    }

    $wingetCmd = Get-Command winget -ErrorAction SilentlyContinue
    if ($wingetCmd) {
        Write-Host "MySQL not found. Trying install via winget..." -ForegroundColor Cyan
        try {
            & winget install -e --id Oracle.MySQL --accept-source-agreements --accept-package-agreements
        }
        catch {
            Write-Host "winget install failed. Will try installer path if provided." -ForegroundColor Yellow
        }
    }

    if (-not (Test-Path $mysqlExe) -and -not [string]::IsNullOrWhiteSpace($InstallerPath)) {
        if (-not (Test-Path $InstallerPath)) {
            throw "Provided MySQL installer file not found: $InstallerPath"
        }

        Write-Host "Trying MySQL installer: $InstallerPath" -ForegroundColor Cyan
        # Works for many MSI/EXE installers supporting /quiet or /silent.
        try {
            Start-Process -FilePath $InstallerPath -ArgumentList "/quiet" -Wait
        }
        catch {
            Start-Process -FilePath $InstallerPath -ArgumentList "/silent" -Wait
        }
    }

    Start-Sleep -Seconds 2
    if (-not (Test-Path $mysqlExe)) {
        throw "MySQL install could not be completed automatically. Install MySQL Server manually, then rerun this script. Expected mysql.exe at: $mysqlExe"
    }

    Write-Host "MySQL installed successfully." -ForegroundColor Green
}

function Restore-Database {
    param(
        [string]$BinPath,
        [string]$Host,
        [int]$Port,
        [string]$User,
        [string]$Password,
        [string]$DbName,
        [string]$SqlFile
    )

    $mysqlExe = Join-Path $BinPath "mysql.exe"
    if (-not (Test-Path $mysqlExe)) {
        throw "mysql.exe not found at: $mysqlExe"
    }

    if (-not (Test-Path $SqlFile)) {
        throw "Backup SQL file not found: $SqlFile"
    }

    $env:MYSQL_PWD = $Password
    try {
        & $mysqlExe --host=$Host --port=$Port --user=$User -e "CREATE DATABASE IF NOT EXISTS $DbName CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
        Get-Content -Path $SqlFile -Raw | & $mysqlExe --host=$Host --port=$Port --user=$User $DbName
    }
    finally {
        Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
    }

    Write-Host "Database restore done: $DbName" -ForegroundColor Green
}

function Start-Application {
    param([string]$Folder, [string]$Exe, [switch]$Shortcut)

    $exePath = Join-Path $Folder $Exe
    if (-not (Test-Path $exePath)) {
        throw "Application EXE not found: $exePath"
    }

    if ($Shortcut) {
        $desktop = [Environment]::GetFolderPath("Desktop")
        $shortcutPath = Join-Path $desktop "Bubby Planet Showroom.lnk"

        $wsh = New-Object -ComObject WScript.Shell
        $sc = $wsh.CreateShortcut($shortcutPath)
        $sc.TargetPath = $exePath
        $sc.WorkingDirectory = $Folder
        $sc.IconLocation = "$exePath,0"
        $sc.Save()

        Write-Host "Shortcut created: $shortcutPath" -ForegroundColor Green
    }

    Write-Host "Launching app..." -ForegroundColor Cyan
    Start-Process -FilePath $exePath -WorkingDirectory $Folder
}

Ensure-Admin

$base = Get-Location
$appDir = Resolve-PathSafe -p $AppFolder -base $base
$sqlPath = Resolve-PathSafe -p $BackupFile -base $base
$installer = Resolve-PathSafe -p $MySqlInstallerPath -base $base

$appDir = (Resolve-Path $appDir).Path
if (-not (Test-Path $sqlPath)) {
    Write-Host "Warning: SQL backup file not found at '$sqlPath'. App will still be launched if DB already exists." -ForegroundColor Yellow
}

Ensure-MySqlPresent -BinPath $MySqlBin -InstallerPath $installer -SkipInstall:$SkipMySqlInstall

if (Test-Path $sqlPath) {
    Restore-Database -BinPath $MySqlBin -Host "localhost" -Port 3306 -User $MySqlUser -Password $MySqlPassword -DbName $Database -SqlFile $sqlPath
}

Start-Application -Folder $appDir -Exe $ExeName -Shortcut:$CreateDesktopShortcut

Write-Host "Setup completed successfully." -ForegroundColor Green
