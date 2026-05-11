param(
    [string]$Channel = "stable",
    [string]$RepoUrl = "https://github.com/JordyRetana/CV",
    [string]$Version = "0.2.1"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    throw "GITHUB_TOKEN is not set. Create a GitHub fine-grained token with Contents read/write permission and set it as an environment variable."
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$releaseDir = Join-Path $projectRoot "artifacts\velopack\$Channel"

$env:DOTNET_ROLL_FORWARD = "Major"

vpk upload github `
    --outputDir $releaseDir `
    --repoUrl $RepoUrl `
    --channel $Channel `
    --releaseName "CV Desktop Editor $Version" `
    --tag "v$Version" `
    --token $env:GITHUB_TOKEN

Write-Host "Uploaded Velopack release v$Version to $RepoUrl"
