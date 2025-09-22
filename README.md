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

## Build from PowerShell (portable EXE)
1. Open **Windows Terminal** or **PowerShell** and change into the repository folder, e.g.:
   ```powershell
   cd C:\path\to\Empty-Folders-Cleaner
   ```
2. Restore NuGet packages:
   ```powershell
   dotnet restore
   ```
3. Publish a self-contained, portable build (no runtime required on the target PC):
   ```powershell
   dotnet publish src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj -c Release -p:PublishProfile=Win-x64-SelfContained
   ```
4. After the command finishes, the portable executable and its dependencies are located at:
   ```
   src/EmptyFolderCleaner.WinUI/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/EmptyFolderCleaner.WinUI.exe
   ```
   Distribute the entire **publish** folder (zipping it works well). The Windows App SDK runtime loads from the OS when available, which is standard for unpackaged WinUI apps.

### PowerShell helper script
Instead of running the commands manually, you can execute the helper script from the repository root. It restores packages, publishes the app by using the `Win-x64-SelfContained` profile, and creates a ZIP archive next to the publish output:

```powershell
pwsh ./publish.ps1
```

You can override the build configuration or skip the ZIP step when needed:

```powershell
pwsh ./publish.ps1 -Configuration Debug -SkipZip
```

## Build an MSIX package (recommended installer)
1. Supply the required visual assets in `src/EmptyFolderCleaner.Packaging/Images` (see the `README.md` inside that folder for the expected file names and pixel sizes).
2. Restore dependencies (only required once per machine):
   ```powershell
   dotnet restore
   ```
3. Use **MSBuild** to create a signed-ready bundle from the Windows App Packaging project:
   ```powershell
   msbuild .\EmptyFolderCleaner.Packaging\EmptyFolderCleaner.Packaging.wapproj `
     /p:Configuration=Release `
     /p:UapAppxPackageBuildMode=StoreUpload `
     /p:GenerateAppxPackageOnBuild=true
   ```
4. The output folder contains an **.msixbundle** plus a temporary certificate (\*.cer). Install the certificate into **Local Machine → Trusted People** (or sign the bundle with your own code-signing certificate), then double-click the MSIX bundle to install.

The MSIX build targets **Windows 10, version 2004 (build 19041)** or later and ships in a single file that installs/uninstalls cleanly. Hosting the bundle alongside an **App Installer** XML enables background updates with no extra tooling.

### Run the app from source
If you just want to launch the app (without producing publish artifacts), run the included helper script from the repository root:

```powershell
pwsh ./run.ps1 -Configuration Release
```

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
