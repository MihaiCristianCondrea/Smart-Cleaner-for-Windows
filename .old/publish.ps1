[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$PublishProfile = 'Win-x64-SelfContained',

    [switch]$SkipZip
)

$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'Empty Folder Cleaner can only be published from Windows because it targets WinUI.'
}

$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot 'src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj'
if (-not (Test-Path $project)) {
    throw "Project file not found at '$project'"
}

$profilePath = Join-Path (Split-Path $project -Parent) "Properties/PublishProfiles/$PublishProfile.pubxml"
if (-not (Test-Path $profilePath)) {
    throw "Publish profile '$PublishProfile' not found at '$profilePath'"
}

Write-Host "Restoring dependencies..." -ForegroundColor Cyan
& dotnet restore $project --nologo | Out-Null

Write-Host "Publishing Empty Folder Cleaner (Configuration=$Configuration, Profile=$PublishProfile)..." -ForegroundColor Cyan
& dotnet publish $project --configuration $Configuration -p:PublishProfile=$PublishProfile --nologo

[xml]$profile = Get-Content $profilePath
$runtimeIdentifier = $profile.Project.PropertyGroup.RuntimeIdentifier
if (-not $runtimeIdentifier) {
    $runtimeIdentifier = 'win-x64'
}

$projectDir = Split-Path $project -Parent
$publishRoot = Join-Path $projectDir "bin/$Configuration"
$publishDirItem = Get-ChildItem -Path $publishRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $ridPublishPath = Join-Path $_.FullName "$runtimeIdentifier/publish"
    if (Test-Path $ridPublishPath) {
        Get-Item $ridPublishPath
    }
} | Sort-Object LastWriteTime | Select-Object -Last 1

if (-not $publishDirItem) {
    throw "Unable to locate publish output under '$publishRoot'."
}

$publishDir = $publishDirItem.FullName
Write-Host "Publish output: $publishDir" -ForegroundColor Green

if ($SkipZip) {
    Write-Host 'Skipping ZIP archive creation.' -ForegroundColor Yellow
    return
}

$zipPath = "$publishDir.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force
Write-Host "ZIP archive: $zipPath" -ForegroundColor Green
