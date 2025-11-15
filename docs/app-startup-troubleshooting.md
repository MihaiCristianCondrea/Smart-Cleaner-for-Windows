# Smart Cleaner for Windows startup troubleshooting

This note summarises startup failures we have diagnosed so far and how to remediate them.

## November 2025 – Directory cleaner static recursion prevents startup

### Symptoms

* MSIX package installs successfully, but the splash screen dismisses immediately and the process exits.
* Event Viewer records a `TypeInitializationException` for `Smart_Cleaner_for_Windows.Core.FileSystem.DirectoryCleaner` thrown during `Program.Main`.

### Root cause

* `DirectoryCleaner` exposed convenience static helpers backed by a `private static IDirectoryCleaner Default` field initialised with `DirectoryCleanerFactory.CreateDefault()`.
* The field initialiser executes while the CLR is running the type initialiser for `DirectoryCleaner`. Calling the factory from within that initialisation path re-enters the same type before it has finished initialising, triggering the `TypeInitializationException` and terminating the process before the main window appears.

### Fix

* Disable trimming for every configuration in `src/SmartCleanerForWindows/SmartCleanerForWindows.csproj`.
* Rebuild and republish the MSIX package so that all WinUI runtime types remain in the bundle.
### Verification checklist

* [ ] App launches to the dashboard when installed from MSIX and when run unpackaged.
* [ ] Empty folders scans still execute and return results.
* [ ] No `TypeInitializationException` entries appear in Event Viewer under **Application**.

## November 2025 – Directory cleaner static recursion prevents startup

### Symptoms

* MSIX package installs successfully, but the splash screen dismisses immediately and the process exits.
* Event Viewer records a `TypeInitializationException` for `Smart_Cleaner_for_Windows.Core.FileSystem.DirectoryCleaner` thrown during `Program.Main`.

### Root cause

* `DirectoryCleaner` exposed convenience static helpers backed by a `private static IDirectoryCleaner Default` field initialised with `DirectoryCleanerFactory.CreateDefault()`.
* The field initialiser executes while the CLR is running the type initialiser for `DirectoryCleaner`. Calling the factory from within that initialisation path re-enters the same type before it has finished initialising, triggering the `TypeInitializationException` and terminating the process before the main window appears.

### Fix

* Defer construction of the shared `DirectoryCleaner` instance with `Lazy<IDirectoryCleaner>` so that the factory only runs after the type initialiser has completed.
* Update the class to resolve the default instance through the lazy wrapper.

### Verification checklist

* [ ] App launches to the dashboard when installed from MSIX and when run unpackaged.
* [ ] Empty folders scans still execute and return results.
* [ ] No `TypeInitializationException` entries appear in Event Viewer under **Application**.

## October 2025 – Desktop backdrop capability probes crash on launch

### Symptoms

* App installs successfully but quits immediately after the splash screen without rendering the main window.
* Event Viewer reports a `NotImplementedException` (or other platform-specific error) thrown while evaluating `DesktopAcrylicController.IsSupported()` during app startup.

### Root cause

* Some Windows builds (and alternative composition hosts) throw from `MicaController.IsSupported()` and `DesktopAcrylicController.IsSupported()` instead of returning `false` when the effect is unavailable.【3546b7†L1-L32】
* Our backdrop initialisation logic called these APIs outside a guard, so the exception escaped the dispatcher startup routine and terminated the process before `MainWindow` could appear.

### Fix

* Wrap the capability probes and backdrop creation in safe helpers that swallow unsupported platform exceptions and fall back to no backdrop.
* Use the helper when instantiating Mica and Desktop Acrylic backdrops during window construction.

### Verification checklist

* [ ] App launches to the dashboard on systems that previously crashed during backdrop detection.
* [ ] Backdrop gracefully falls back to `null` when unsupported without terminating the process.
* [ ] Existing Mica/legacy Mica behaviour still applies on supported systems.

## October 2025 – Nullable cancellation tokens crash the app

### Symptoms

* MSIX package installs successfully, but the splash screen dismisses immediately and the process exits before the main window appears.
* Event Viewer shows an `InvalidOperationException: Nullable object must have a value` thrown from `MainWindow.UpdateStorageOverviewAsync`.

### Root cause

* The dashboard refresh logic awaited `_storageOverviewCts?.CancelAsync()!`. When the field was `null`, the compiler attempted to await a `ValueTask?`, triggering the exception before the first frame could render.
* Disk cleanup commands reused the same pattern, so clicking **Analyze** or **Clean** would also terminate the process if a previous CTS was not present.

### Fix

* Guard cancellation and disposal with a shared helper that only calls `CancelAsync` when a token source exists, handles disposal, and preserves any in-flight operations.
* Update the storage overview and disk cleanup workflows to use the helper and reference the locally created `CancellationTokenSource` throughout the request lifecycle.

### Verification checklist

* [ ] Launches to the dashboard without crashing.
* [ ] Storage overview populates drive usage and surfaces an error banner instead of crashing when access is denied.
* [ ] Disk cleanup **Analyze** and **Clean** commands run, can be cancelled, and no longer terminate the app.

## September 2025 – IL trimming removes WinUI runtime types

### Symptoms

* The packaged build installed correctly, but the process exited immediately after showing the splash screen.
* Event Viewer recorded a missing WinRT projection type when the entry assembly initialised WinUI.

### Root cause

* The Release publish profile enabled IL trimming (`PublishTrimmed=true`).
* WinUI 3 relies on reflection-heavy generated code that the .NET trimmer cannot analyse, so required runtime types were removed from the package.

### Fix

* Disable trimming for every configuration in `src/SmartCleanerForWindows/SmartCleanerForWindows.csproj`.
* Rebuild and republish the MSIX package so that all WinUI runtime types remain in the bundle.

### Verification checklist

* [ ] Build succeeds in both Debug and Release configurations.
* [ ] Packaged app launches to the dashboard view.
* [ ] Regression tests (if any) pass.
