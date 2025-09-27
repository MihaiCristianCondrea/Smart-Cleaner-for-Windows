# Smart Cleaner for Windows (WinUI 3)

Smart Cleaner for Windows is a modern WinUI 3 desktop utility that helps you tidy up empty folders, analyze built-in Windows Disk Cleanup handlers, and keep storage usage in check. The app is built with **.NET 8** and the **Windows App SDK 1.8** to deliver Fluent visuals, Mica materials, and responsive layouts that respect your accent color and theme preferences.

## Highlights
- **Dashboard overview** – review drive usage at a glance and jump directly into the cleanup tools from a Fluent NavigationView.
- **Empty folders cleaner** – preview every empty directory before deletion, send removals to the **Recycle Bin** by default, cap traversal depth, skip reparse points, cancel long scans, and exclude folders with wildcard rules such as `.git; build/*; node_modules`.
- **Disk cleanup integration** – inspect the Windows Disk Cleanup handlers for a volume, see the estimated size reclaimed per category, review warnings (including elevation requirements), and clean only the items you select.
- **Personal and polished** – supports English (United States) and Spanish (Spain), includes theme/accent controls, and keeps all operations on-device with no telemetry.

## Requirements
- Windows 10 version 1809 (build 17763) or later. The project targets `net8.0-windows10.0.19041.0` while maintaining a minimum platform version of 17763 in the package manifest.
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) for command-line builds.
- (Optional) [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.10 or later with the **Windows App SDK C#** workload for an IDE experience.

## Repository layout
| Path | Description |
| --- | --- |
| `Smart Cleaner for Windows/` | WinUI 3 single-project desktop application (XAML, view models, and assets). |
| `Smart Cleaner for Windows/Core/` | Cross-platform logic for scanning directories and integrating with the Windows Disk Cleanup COM handlers. |
| `Smart Cleaner for Windows/Strings/` | Localized resources (`en-US` and `es-ES`). |
| `Smart Cleaner for Windows/Properties/PublishProfiles/` | Publish profiles for x86, x64, and ARM64 self-contained builds. |
| `docs/SOLID-refactoring-guide.md` | Architecture roadmap for breaking the monolithic window into SOLID-aligned modules. |

## Getting started
### 1. Clone the repository
```powershell
git clone https://github.com/<your-account>/Smart-Cleaner-for-Windows.git
cd Smart-Cleaner-for-Windows
```

### 2. Restore dependencies
```powershell
dotnet restore
```

### 3. Build the solution
```powershell
dotnet build "Smart Cleaner for Windows.sln" -c Release
```

### 4. Run from the command line
```powershell
dotnet run --project "Smart Cleaner for Windows/Smart Cleaner for Windows.csproj" -c Debug
```
Alternatively, open `Smart Cleaner for Windows.sln` in Visual Studio and press **F5**.

## Publish a self-contained build
Use the supplied publish profiles to create portable outputs that do not require the .NET runtime on the target machine:

```powershell
dotnet publish "Smart Cleaner for Windows/Smart Cleaner for Windows.csproj" -c Release -p:PublishProfile=win-x64
```

The resulting files live in:
```
Smart Cleaner for Windows/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/
```
Distribute the entire **publish** folder (zipping it works well). Switch to `win-x86` or `win-arm64` to generate binaries for other architectures.

### Choose a Windows App SDK channel
The project references stable, preview, and experimental channels. Override the default (stable) channel with an MSBuild property:

```powershell
dotnet build "Smart Cleaner for Windows/Smart Cleaner for Windows.csproj" -p:WindowsAppSdkChannel=preview
dotnet publish "Smart Cleaner for Windows/Smart Cleaner for Windows.csproj" -c Release -p:PublishProfile=win-x64 -p:WindowsAppSdkChannel=experimental
```

### Package as MSIX
Smart Cleaner for Windows enables the single-project MSIX tooling. To produce an installer:

1. Open the solution in Visual Studio on Windows.
2. Right-click **Smart Cleaner for Windows** and select **Package and Publish → Create App Packages...**.
3. Follow the wizard to generate an `.msixbundle` and sign it with a trusted certificate.

Advanced users can also run `dotnet publish ... -p:WindowsPackageType=Msix` from PowerShell to create an unsigned MSIX for sideloading.

## Using Smart Cleaner
### Empty folders cleaner
1. Select **Empty folders** from the navigation menu.
2. Choose a root directory and adjust options such as **Recycle Bin**, **Depth limit**, and **Exclusions**.
3. Click **Preview** to list empty directories and review the results.
4. Use **Clean now** to remove them or **Cancel** to stop a long-running scan.

### Disk cleanup
1. Switch to **Disk cleanup**.
2. Click **Analyze** to enumerate Windows cleanup handlers for the default drive (you can change the drive in settings).
3. Select the categories you want to remove, paying attention to elevation requirements or warnings.
4. Choose **Clean selected** to reclaim space. Results summarize the freed space and any failures.

### Personalization
Open **Settings** to switch between light, dark, or system theme; apply the custom “Zest” accent; and view app information.

## Localization
The app ships with resource files for **English (United States)** and **Spanish (Spain)**. Add additional `.resw` files under `Strings/` to extend localization coverage.

## Privacy
All scans and cleanups happen locally on your machine. The app does not collect telemetry or send data over the network.

## Credits
- Windows App SDK / WinUI 3
- Windows Community Toolkit (implicit animations and helpers)
- Fluent Design System guidance
