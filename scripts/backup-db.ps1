param(
    [string]$MySqlBin = "C:\\Program Files\\MySQL\\MySQL Server 8.0\\bin",
    [string]$Host = "localhost",
    [int]$Port = 3306,
    [string]$User = "root",
    [string]$Password = "",
    [string]$Database = "showroom_db",
    [string]$OutputFile = "db-backup-showroom_db.sql"
)

$ErrorActionPreference = "Stop"

$dumpExe = Join-Path $MySqlBin "mysqldump.exe"
if (-not (Test-Path $dumpExe)) {
    throw "mysqldump.exe not found at: $dumpExe"
}

$target = Join-Path (Join-Path $PSScriptRoot "..") $OutputFile

$env:MYSQL_PWD = $Password
try {
    & $dumpExe --host=$Host --port=$Port --user=$User --databases $Database --routines --triggers --events --single-transaction --set-gtid-purged=OFF --result-file="$target"
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}

Write-Host "DB backup created: $target" -ForegroundColor Green
