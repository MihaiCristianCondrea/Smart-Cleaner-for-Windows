# Empty Folder Cleaner (Windows, WinUI 3)

A fast, safe, and modern Windows app to find and delete empty folders. Built with **.NET 8** and the **Windows App SDK (WinUI 3)** for a Fluent look that respects the system accent, uses Mica, and stays responsive during long scans.

## Features
- **Preview first** – scan any folder and review empty directories before you delete them.
- **Safe by default** – deletions go to the **Recycle Bin** (you can opt out for permanent removal).
- **Exclusions** – semicolon separated wildcards (e.g., `.git; build/*; node_modules`).
- **Depth limit** – constrain traversal depth (0 = unlimited).
- **Symlink aware** – reparse points are skipped by default.
- **Progress + cancel** – long operations stay cancellable and surface status in an InfoBar.
- **Fluent UI** – Mica backdrop, accent-aware buttons, and light implicit animations.

## Requirements
- Windows 10 2004 (build 19041) or newer.
- No additional runtime is required when you use the self-contained publish output.

## Build (portable publish)
```powershell
dotnet restore
dotnet publish src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj -c Release -p:PublishProfile=Win-x64-SelfContained
```

The resulting executable lives in:

```
src/EmptyFolderCleaner.WinUI/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/EmptyFolderCleaner.WinUI.exe
```

Distribute the entire publish folder (a zip works well). The Windows App SDK runtime loads from the OS when available, which is standard for unpackaged WinUI apps.

To automate publishing on Windows, run:

```powershell
pwsh src/EmptyFolderCleaner.WinUI/publish.ps1
```

The script restores dependencies, publishes, and zips the latest output.

## Usage
1. Launch **Empty Folder Cleaner**.
2. Browse to the root directory you want to inspect.
3. Adjust exclusion patterns or depth if needed.
4. Click **Preview** to list empty folders.
5. Review the results, then click **Delete** to remove them (Recycle Bin by default).
6. Use **Cancel** whenever you want to stop a long-running scan.

## Privacy
Everything runs locally. The app only reads and deletes directories that you point it to—no telemetry, no uploads.

## Credits
- Windows App SDK / WinUI 3
- Windows Community Toolkit (animations)
