param(
    [string]$Version = "0.4.9",
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
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot "artifacts")) + [IO.Path]::DirectorySeparatorChar

foreach ($target in @($publishDir, $releaseDir, $clientReleaseDir)) {
    $resolved = [IO.Path]::GetFullPath($target)
    if (-not $resolved.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated path escapes the artifacts directory: $resolved"
    }
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $releaseDir) {
    Remove-Item -LiteralPath $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDir | Out-Null

& (Join-Path $projectRoot "scripts\build-velopack.ps1") -Version $Version -Channel stable
if (-not (Test-Path -LiteralPath $clientInstallerPath)) { throw "Client installer was not created." }

dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
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
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$clientInstallerFolder = Join-Path $publishDir "ClientInstaller"
New-Item -ItemType Directory -Path $clientInstallerFolder -Force | Out-Null
Copy-Item -LiteralPath $clientInstallerPath -Destination (Join-Path $clientInstallerFolder "CVDesktopEditor-stable-Setup.exe") -Force

$env:DOTNET_ROLL_FORWARD = "Major"

vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --packTitle "CV Desktop Editor Admin" `
    --packAuthors "Jordy Retana" `
    --icon (Join-Path $projectRoot "Assets\appicon.ico") `
    --channel $Channel `
    --packDir $publishDir `
    --mainExe "$PackId.exe" `
    --outputDir $releaseDir
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

Write-Host "Admin Velopack release created in $releaseDir"
