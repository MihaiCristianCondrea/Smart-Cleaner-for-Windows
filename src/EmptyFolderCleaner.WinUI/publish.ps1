[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$PublishProfile = 'Win-x64-SelfContained',

    [switch]$SkipZip
)

$ErrorActionPreference = 'Stop'

$repoScript = Join-Path $PSScriptRoot '..' '..' 'publish.ps1'
if (-not (Test-Path $repoScript)) {
    throw "Unable to locate publish helper script at '$repoScript'."
}

& $repoScript -Configuration $Configuration -PublishProfile $PublishProfile -SkipZip:$SkipZip
