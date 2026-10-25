param(
    [string]$Version = "0.4.15",
    [string]$PackId = "CVDesktopEditor",
    [string]$Runtime = "win-x64",
    [string]$Channel = "stable"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "artifacts\publish"
$releaseDir = Join-Path $projectRoot "artifacts\velopack\$Channel"
$projectPath = Join-Path $projectRoot "CVDesktopEditor.csproj"
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot "artifacts")) + [IO.Path]::DirectorySeparatorChar

foreach ($target in @($publishDir, $releaseDir)) {
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

dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    /p:Version=$Version `
    /p:AssemblyVersion=$Version.0 `
    /p:FileVersion=$Version.0
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$env:DOTNET_ROLL_FORWARD = "Major"

vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --packTitle "CV Desktop Editor" `
    --packAuthors "Jordy Retana" `
    --icon (Join-Path $projectRoot "Assets\appicon.ico") `
    --channel $Channel `
    --packDir $publishDir `
    --mainExe "CVDesktopEditor.exe" `
    --outputDir $releaseDir
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

Write-Host "Velopack release created in $releaseDir"
