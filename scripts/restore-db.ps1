param(
    [string]$MySqlBin = "C:\\Program Files\\MySQL\\MySQL Server 8.0\\bin",
    [string]$Host = "localhost",
    [int]$Port = 3306,
    [string]$User = "root",
    [string]$Password = "",
    [string]$Database = "showroom_db",
    [string]$BackupFile = "db-backup-showroom_db.sql"
)

$ErrorActionPreference = "Stop"

$mysqlExe = Join-Path $MySqlBin "mysql.exe"
if (-not (Test-Path $mysqlExe)) {
    throw "mysql.exe not found at: $mysqlExe"
}

$backupPath = if ([System.IO.Path]::IsPathRooted($BackupFile)) { $BackupFile } else { Join-Path (Join-Path $PSScriptRoot "..") $BackupFile }
if (-not (Test-Path $backupPath)) {
    throw "Backup file not found: $backupPath"
}

$env:MYSQL_PWD = $Password
try {
    & $mysqlExe --host=$Host --port=$Port --user=$User -e "CREATE DATABASE IF NOT EXISTS $Database CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
    Get-Content -Path $backupPath -Raw | & $mysqlExe --host=$Host --port=$Port --user=$User $Database
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}

Write-Host "DB restore complete into '$Database'." -ForegroundColor Green
