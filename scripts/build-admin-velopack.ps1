param(
    [string]$Version = "0.3.0",
    [string]$PackId = "CVDesktopEditorAdmin",
    [string]$Runtime = "win-x64",
    [string]$Channel = "admin"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "artifacts\publish-admin"
$releaseDir = Join-Path $projectRoot "artifacts\velopack\$Channel"
$projectPath = Join-Path $projectRoot "CVDesktopEditor.csproj"
$clientReleaseDir = Join-Path $projectRoot "artifacts\velopack\stable"
$clientInstallerPath = Join-Path $clientReleaseDir "CVDesktopEditor-stable-Setup.exe"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $releaseDir) {
    Remove-Item -LiteralPath $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDir | Out-Null

if (-not (Test-Path $clientInstallerPath)) {
    & (Join-Path $projectRoot "scripts\build-velopack.ps1") -Version $Version -Channel stable
}

dotnet restore $projectPath
dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    /p:Version=$Version `
    /p:AssemblyVersion=$Version.0 `
    /p:FileVersion=$Version.0 `
    /p:DefineConstants=ADMIN_BUILD `
    /p:AssemblyName=$PackId `
    /p:Product="CV Desktop Editor Admin"

$clientInstallerFolder = Join-Path $publishDir "ClientInstaller"
New-Item -ItemType Directory -Path $clientInstallerFolder -Force | Out-Null
Copy-Item -LiteralPath $clientInstallerPath -Destination (Join-Path $clientInstallerFolder "CVDesktopEditor-stable-Setup.exe") -Force

$env:DOTNET_ROLL_FORWARD = "Major"

vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --channel $Channel `
    --packDir $publishDir `
    --mainExe "$PackId.exe" `
    --outputDir $releaseDir

Write-Host "Admin Velopack release created in $releaseDir"
