param(
    [string]$AppFolder = ".",
    [string]$ExeName = "BubbyPlanetShowroom.exe",
    [switch]$CreateDesktopShortcut
)

$ErrorActionPreference = "Stop"

$resolvedFolder = if ([System.IO.Path]::IsPathRooted($AppFolder)) { $AppFolder } else { Join-Path (Get-Location) $AppFolder }
$resolvedFolder = (Resolve-Path $resolvedFolder).Path
$exePath = Join-Path $resolvedFolder $ExeName

if (-not (Test-Path $exePath)) {
    throw "App EXE not found: $exePath"
}

Write-Host "App found: $exePath" -ForegroundColor Green

if ($CreateDesktopShortcut) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktop "Bubby Planet Showroom.lnk"

    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $resolvedFolder
    $shortcut.IconLocation = "$exePath,0"
    $shortcut.Save()

    Write-Host "Desktop shortcut created: $shortcutPath" -ForegroundColor Green
}

Write-Host "Launching app..." -ForegroundColor Cyan
Start-Process -FilePath $exePath -WorkingDirectory $resolvedFolder
