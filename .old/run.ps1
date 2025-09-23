[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'Empty Folder Cleaner can only run on Windows 10 build 19041 or later.'
}

$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot 'src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj'

if (-not (Test-Path $project)) {
    throw "Project file not found at '$project'"
}

Write-Host "Restoring dependencies..." -ForegroundColor Cyan
& dotnet restore $project | Out-Null

Write-Host "Launching Empty Folder Cleaner using configuration '$Configuration'..." -ForegroundColor Cyan
& dotnet run --project $project --configuration $Configuration --no-restore
