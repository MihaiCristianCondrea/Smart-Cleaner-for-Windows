# Settings namespace investigation

## Observed IDE failure
- `MainWindow.xaml.cs` references `SmartCleanerForWindows.Settings` to wire the shared settings service into the shell, but that namespace does not exist anywhere in the repository, so Rider reports *Cannot resolve symbol 'SmartCleanerForWindows.Settings'* when loading the project. The file imports the namespace at the top of the file, so the error is immediate during design-time compilation.

## Current Smart Cleaner structure
```
src/
  SmartCleanerForWindows/                 # WinUI host shell + shared settings library (src/SmartCleanerForWindows/Settings)
  SmartCleanerForWindows.SettingsUi/      # Standalone settings surface
  SettingsDefinitions/                    # Tool JSON schema files
```
- Until this change the settings library's project file set `RootNamespace` and `AssemblyName` to `SmartCleaner.Settings`, so the compiler emitted types under `SmartCleaner.Settings.*` even though the rest of the solution uses the `SmartCleanerForWindows.*` root namespace.

## Inspiration baseline (PowerToys)
- PowerToys keeps its WinUI settings app in `src/settings-ui/Settings.UI/PowerToys.Settings.csproj` where the root namespace is `Microsoft.PowerToys.Settings.UI`, matching the project folder and how the rest of PowerToys references the assembly. Every module references `Microsoft.PowerToys.Settings.UI` (or the library) so namespace resolution is consistent.

## Conclusion
- Because Smart Cleaner renamed only the folder but left the namespace/assembly as `SmartCleaner.Settings`, the shell tries to import a namespace that never compiles, causing Rider to show the IDE error. Aligning the project name, root namespace, and assembly to `SmartCleanerForWindows.Settings` (mirroring how PowerToys aligns `Microsoft.PowerToys.Settings.UI`) resolves the mismatch without further code changes. Keeping the project under `src/SmartCleanerForWindows/Settings` also ensures IDEs that open only the shell folder still load the shared library and surface the namespace.
