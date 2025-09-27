# Smart Cleaner SOLID Refactoring Guide

## Why refactoring is needed
- The shell `MainWindow.xaml` currently embeds the dashboard, empty folder cleaner, disk cleanup, large files explorer, and settings panes in a single view definition, creating a 1,600-line XAML file that is difficult to evolve feature-by-feature.【F:Smart Cleaner for Windows/MainWindow.xaml†L74-L1662】
- Each cleaner workflow is orchestrated directly from the `MainWindow` partial classes, so UI event handlers like `OnPreview`, `OnDelete`, and `OnDiskCleanupClean` mix presentation logic, orchestration, and result messaging in the same class.【F:Smart Cleaner for Windows/MainWindow.EmptyFolders.cs†L14-L252】【F:Smart Cleaner for Windows/MainWindow.DiskCleanup.cs†L15-L262】
- Core services such as `DirectoryCleaner` encapsulate empty-folder traversal and deletion, but the type still combines enumeration, exclusion parsing, and deletion policies, leaving little room for alternative storage heuristics or test seams.【F:Smart Cleaner for Windows/Core/DirectoryCleaner.cs†L13-L347】

These hotspots violate the Single Responsibility, Interface Segregation, and Dependency Inversion principles by forcing wide knowledge of UI and infrastructure details inside a few classes. The goal of this guide is to supply a step-by-step plan for decomposing the app into smaller, SOLID-friendly components.

## Guiding principles refresher
1. **Single Responsibility** – every class or component owns one reason to change.
2. **Open/Closed** – existing types stay stable; new behaviour arrives through extension points.
3. **Liskov Substitution** – abstractions guarantee that specialised implementations can stand in for their base contracts.
4. **Interface Segregation** – consumers depend on focused interfaces tailored to their needs.
5. **Dependency Inversion** – high-level workflows talk to abstractions, while low-level services implement them.

## Target architecture
Break the monolith into feature-oriented modules that align with each navigation item:

| Area | Current state | Refactoring destination |
| --- | --- | --- |
| Dashboard | Static overview assembled inside `MainWindow.xaml`.【F:Smart Cleaner for Windows/MainWindow.xaml†L74-L200】 | Create a `DashboardView` user control backed by a `DashboardViewModel` that exposes drive summaries and storage tips via an injected `IStorageOverviewService`. |
| Empty folders | UI and orchestration mixed in `MainWindow.EmptyFolders.cs`, with direct references to `DirectoryCleaner` and UI elements.【F:Smart Cleaner for Windows/MainWindow.EmptyFolders.cs†L14-L252】 | Extract a `EmptyFolderScanView` control and a presenter/service combo (`IEmptyFolderScanCoordinator`). Surface progress and result models via data binding rather than manipulating UI elements directly. |
| Large files | Visual tree and settings live beside other views in XAML, but logic is spread across the main window partials (not shown above).【F:Smart Cleaner for Windows/MainWindow.xaml†L820-L1123】 | Mirror the pattern used for empty folders: isolate view logic and run scans through an `ILargeFileAnalyzer` abstraction. |
| Disk cleanup | Handler analysis and cleaning logic built into the window partial class.【F:Smart Cleaner for Windows/MainWindow.DiskCleanup.cs†L15-L262】 | Promote a `DiskCleanupView` and coordinator service that encapsulate analyze/clean loops, exposing bindable state (`IsBusy`, `SelectedHandlers`, `Summary`). |
| Settings | Only personalization, automation, notification, and history toggles are available, leaving no space for defining cleaner defaults per tool or automation recipes.【F:Smart Cleaner for Windows/MainWindow.xaml†L1290-L1658】 | Split settings into domain-specific sections (`CleanerDefaultsSettings`, `AutomationSettings`, etc.) and introduce configuration models that can be persisted and injected into each tool. |

## Applying SOLID in practice

### 1. Single Responsibility
- **View composition**: Replace the mega-XAML with dedicated `UserControl` files (`DashboardView.xaml`, `EmptyFoldersView.xaml`, etc.) and use the navigation view only to swap controls. This keeps layout changes local to each feature.【F:Smart Cleaner for Windows/MainWindow.xaml†L74-L1662】
- **Coordinator services**: Move workflow orchestration out of the code-behind. For example, create an `EmptyFolderCoordinator` class that receives `IDirectoryCleaner`, handles cancellation, and publishes `ScanState` objects that the view binds to. The code-behind then only translates UI events into coordinator calls.【F:Smart Cleaner for Windows/MainWindow.EmptyFolders.cs†L14-L252】
- **Configuration management**: Group related preference toggles into POCO records (e.g., `CleanerDefaults`, `AutomationPreferences`) so the settings page edits models rather than juggling UI elements.

### 2. Open/Closed Principle
- Introduce strategy interfaces for newly planned features—large-file detection or duplicate discovery—while keeping the navigation shell unchanged. `MainWindow` should only depend on an `IWorkspaceModule` collection, allowing new modules to register themselves without modifying the shell.
- When extending cleaners (e.g., adding a “large file” algorithm), implement new `ICleanerTool` derivations instead of editing existing handlers.

### 3. Liskov Substitution
- Ensure every abstraction used by the shell is replaceable. For instance, guarantee `IDiskCleanupService` implementations share the same cancellation semantics exposed in `OnDiskCleanupAnalyze` and `OnDiskCleanupClean` so test doubles remain compatible.【F:Smart Cleaner for Windows/MainWindow.DiskCleanup.cs†L15-L162】
- Model optional capabilities (like requiring elevation) via additional interfaces or capability flags rather than type checks in the UI layer.

### 4. Interface Segregation
- Split the `DirectoryCleaner` contract into fine-grained interfaces: `IDirectoryEnumerator`, `IEmptyDirectoryDetector`, and `IDirectoryDeletionService`. The empty-folder module only needs enumeration and deletion, while future features such as “large file cleanup” can reuse the enumerator without inheriting deletion semantics.【F:Smart Cleaner for Windows/Core/DirectoryCleaner.cs†L13-L347】
- Partition view models so that dashboard tiles bind to read-only projection interfaces (`IDriveUsageSnapshot`) instead of the entire service, avoiding accidental coupling to write operations.【F:Smart Cleaner for Windows/MainWindow.xaml†L100-L167】

### 5. Dependency Inversion
- Register every coordinator/service in the DI container (App.xaml.cs). The window receives abstractions—`IEmptyFolderCoordinator`, `IDiskCleanupCoordinator`, etc.—instead of concrete classes, aligning with the existing pattern of injecting `IDirectoryCleaner` and `IDiskCleanupService` via constructors.【F:Smart Cleaner for Windows/MainWindow.xaml.cs†L128-L215】
- Move infrastructure-specific operations (e.g., file pickers, disk cleanup handlers) behind interfaces so that unit tests can supply in-memory implementations. `DirectoryCleaner` already accepts abstractions like `IDirectorySystem`; extend this approach to UI-triggered workflows.【F:Smart Cleaner for Windows/Core/DirectoryCleaner.cs†L13-L125】【F:Smart Cleaner for Windows/MainWindow.EmptyFolders.cs†L14-L252】

## Step-by-step refactoring roadmap
1. **Modularize the shell**
   - Create separate `UserControl` files for each navigation destination and replace `MainWindow`’s content with a content host bound to view models.
   - Introduce a lightweight `INavigationModule` interface describing each feature’s title, icon, and view factory; register modules during app startup.
2. **Extract coordinators**
   - Move the logic inside `OnPreview`/`OnDelete` into an `EmptyFolderCoordinator` class. Expose commands (`PreviewAsync`, `CleanAsync`, `Cancel`) and observable state (`IsBusy`, `Status`, `Results`). Update bindings to rely on `INotifyPropertyChanged` instead of direct control manipulation.【F:Smart Cleaner for Windows/MainWindow.EmptyFolders.cs†L14-L252】
   - Apply the same pattern to disk cleanup by moving analysis and cleaning into a `DiskCleanupCoordinator`.【F:Smart Cleaner for Windows/MainWindow.DiskCleanup.cs†L15-L262】
3. **Refine core services**
   - Decompose `DirectoryCleaner` into smaller collaborators (`DirectoryTraversalService`, `DirectoryDeletionService`, `ExclusionEvaluator`). Keep the public facade for backwards compatibility while delegating work to the new components.【F:Smart Cleaner for Windows/Core/DirectoryCleaner.cs†L13-L347】
   - Define new interfaces for future cleaners (large files, duplicates) so new heuristics can reuse traversal logic without touching existing deletion code.
4. **Modernize settings management**
   - Bind the settings page to a `SettingsViewModel` that aggregates strongly typed preference objects. Persist them via an `ISettingsStore` abstraction that can later support roaming profiles or cloud sync.
   - Expand the settings surface to let users define per-cleaner defaults (e.g., depth limits, handler exclusions) by editing models rather than direct UI fields.【F:Smart Cleaner for Windows/MainWindow.xaml†L1340-L1658】
5. **Strengthen testing hooks**
   - With coordinators and services split out, add unit tests that exercise scanning, deletion, and disk cleanup flows through the new interfaces.
   - Provide fake implementations of `IDirectorySystem`, `IDiskCleanupService`, and future `ILargeFileAnalyzer` to validate workflows without touching the real file system.【F:Smart Cleaner for Windows/Core/DirectoryCleaner.cs†L13-L347】

## Using this guide with AI tooling
When prompting your AI assistant:
- Reference the module or interface name you want it to edit (e.g., “Update `EmptyFolderCoordinator` to expose a `ScanDepth` property”).
- Provide the relevant SOLID rule from this guide so the assistant keeps logic isolated (e.g., “Follow Interface Segregation by keeping UI code separate from directory traversal”).
- Ask for unit tests whenever you introduce a new abstraction to maintain confidence during iterative refactoring.

By following this blueprint, Smart Cleaner will evolve from a monolithic window into a set of maintainable modules that are easier to extend with new tools, automation, and analytics, while keeping the codebase approachable for both humans and AI copilots.
