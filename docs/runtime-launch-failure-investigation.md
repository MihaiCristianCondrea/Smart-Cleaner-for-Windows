# Runtime launch failure investigation (MSIX opens splash then exits)

## Problem summary

The app could build and install, but the packaged executable exited right after the splash screen without opening the main window.

## What we changed in code

1. Reverted the desktop app target framework from `.NET 10` to `.NET 9` in `SmartCleanerForWindows.csproj`.
2. Updated `global.json` to pin a stable .NET 9 SDK (`9.0.300`) and disabled prerelease SDK selection.

These changes align the runtime and tooling with the project's documented requirements and remove accidental dependency on preview .NET bits.

## Why this can cause "splash then nothing"

When packaged WinUI apps target a preview runtime/toolchain, end-user systems frequently miss one or more runtime prerequisites expected by that preview stack. The process can terminate before a visible window appears, especially when startup fails in runtime initialization or generated UI setup.

Using the stable .NET 9 + Windows App SDK combination reduces this failure class and matches what is documented in this repository.

## Online references checked

- Windows App SDK release channels and support lifecycle docs:
  - https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/stable-channel
  - https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels
- Windows App SDK system requirements landing page:
  - https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-requirements

## Local validation steps (run on a Windows dev machine)

```powershell
# from repo root
dotnet --info
dotnet restore
dotnet build "Smart Cleaner for Windows.sln" -c Release

dotnet publish "src/SmartCleanerForWindows/SmartCleanerForWindows.csproj" `
  -c Release -p:PublishProfile=win-x64
```

Then install and launch the resulting package/app output and inspect logs in:

`%LocalAppData%\SmartCleanerForWindows\Logs\`

Focus on `startup.log` and `crash.log` if startup still fails.
