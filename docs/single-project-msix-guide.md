# Single-project MSIX guide (WinUI 3)

This repository uses **single-project MSIX** for the WinUI desktop app (`src/SmartCleanerForWindows`).

## What single-project MSIX means

Instead of using a separate Windows Application Packaging Project, packaging is configured directly in the app `.csproj`.

In this repo, the app project already includes the key settings:

- `<UseWinUI>true</UseWinUI>`
- `<EnableMsixTooling>true</EnableMsixTooling>`
- `<PublishProfile>win-$(Platform).pubxml</PublishProfile>`

## Prerequisites

- Windows 10/11 development machine
- Visual Studio 2022 with Windows App SDK support
- .NET 9 SDK

## Build the app (without packaging)

From repository root:

```powershell
dotnet restore

dotnet build "Smart Cleaner for Windows.sln" -c Debug
```

## Build + generate MSIX from command line

For single-project MSIX, use:

- `/p:GenerateAppxPackageOnBuild=true`

Example (`x64`, Release):

```powershell
dotnet msbuild "src/SmartCleanerForWindows/SmartCleanerForWindows.csproj" `
  /t:Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true
```

If you omit `GenerateAppxPackageOnBuild=true`, the project still builds, but no MSIX package is produced.

## Build + publish using profile

```powershell
dotnet publish "src/SmartCleanerForWindows/SmartCleanerForWindows.csproj" -c Release -p:PublishProfile=win-x64
```

## Visual Studio workflow

1. Open solution in Visual Studio.
2. Ensure **Deploy** is enabled for your active configuration/platform in **Build > Configuration Manager**.
3. Build and run (F5), or use **Package and Publish** to produce distributable artifacts.

## Notes and limitations

- Single-project MSIX currently produces a single `.msix` package by default in automated command-line flows.
- If you need multi-executable packaging in one package, use a separate packaging project workflow.

## Related deployment/process guidance

For a repo-specific summary of packaged vs unpackaged decisions, package identity implications, and process model notes (AppContainer vs Medium IL), see [`docs/windows-packaging-deployment-process.md`](windows-packaging-deployment-process.md).
