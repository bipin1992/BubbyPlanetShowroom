param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent,
    [switch]$SingleFile,
    [string]$OutputRoot = "publish-output"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\BubbyPlanetShowroom.csproj"
$project = (Resolve-Path $project).Path

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK not found. Install .NET SDK on build machine."
}

$mode = if ($FrameworkDependent) { "framework-dependent" } else { "self-contained" }
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path (Join-Path $PSScriptRoot "..") "$OutputRoot\$mode-$Runtime-$stamp"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host "Publishing app ($mode) ..." -ForegroundColor Cyan

$singleFileValue = if ($PSBoundParameters.ContainsKey("SingleFile")) { [bool]$SingleFile } else { $true }

$publishArgs = @(
    "publish", $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $outDir,
    ("/p:PublishSingleFile=" + ($(if ($singleFileValue) { "true" } else { "false" }))),
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:DebugType=None",
    "/p:DebugSymbols=false"
)

if ($FrameworkDependent) {
    $publishArgs += "--self-contained"
    $publishArgs += "false"
} else {
    $publishArgs += "--self-contained"
    $publishArgs += "true"
}

dotnet @publishArgs

$zipPath = "$outDir.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath

Write-Host "Publish complete:" -ForegroundColor Green
Write-Host "Folder: $outDir"
Write-Host "ZIP   : $zipPath"

Write-Host "\nNext steps:" -ForegroundColor Yellow
Write-Host "1) Copy ZIP to target machine and extract."
Write-Host "2) Setup MySQL + restore DB using scripts\\restore-db.ps1 on target machine."
Write-Host "3) Run scripts\\setup-target.ps1 to launch and create Desktop shortcut."
