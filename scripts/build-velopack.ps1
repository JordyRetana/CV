param(
    [string]$Version = "0.2.0",
    [string]$PackId = "CVDesktopEditor",
    [string]$Runtime = "win-x64",
    [string]$Channel = "stable"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "artifacts\publish"
$releaseDir = Join-Path $projectRoot "artifacts\velopack\$Channel"
$projectPath = Join-Path $projectRoot "CVDesktopEditor.csproj"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $releaseDir) {
    Remove-Item -LiteralPath $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDir | Out-Null

dotnet restore $projectPath
dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    /p:Version=$Version `
    /p:AssemblyVersion=$Version.0 `
    /p:FileVersion=$Version.0

$env:DOTNET_ROLL_FORWARD = "Major"

vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --channel $Channel `
    --packDir $publishDir `
    --mainExe "CVDesktopEditor.exe" `
    --outputDir $releaseDir

Write-Host "Velopack release created in $releaseDir"
