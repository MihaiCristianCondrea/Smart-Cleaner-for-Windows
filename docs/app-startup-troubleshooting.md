# Smart Cleaner for Windows startup troubleshooting

This note summarises startup failures we have diagnosed so far and how to remediate them.

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

* Disable trimming for every configuration in `Smart Cleaner for Windows.csproj`.
* Rebuild and republish the MSIX package so that all WinUI runtime types remain in the bundle.

### Verification checklist

* [ ] Build succeeds in both Debug and Release configurations.
* [ ] Packaged app launches to the dashboard view.
* [ ] Regression tests (if any) pass.
