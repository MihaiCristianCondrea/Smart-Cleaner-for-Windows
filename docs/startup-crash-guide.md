# Startup crash guide (XAML parse failure)

## Symptoms
- App crashes during launch with `Microsoft.UI.Xaml.Markup.XamlParseException: XAML parsing failed`.
- First-chance `System.IO.FileNotFoundException` appears before the XAML error, indicating a missing resource file.

## Root cause
The XAML loader is throwing `FileNotFoundException` during `MainWindow.InitializeComponent()` (first-chance) and then surfacing a `XamlParseException`. That indicates a markup reference to a missing file (e.g., resource dictionary, asset, or control). The exact culprit can vary between builds, so we now capture richer diagnostics when the load fails.

## Fix applied
- Added guarded XAML initialization with detailed logging of the missing file/parse error and a fallback shell so the app no longer terminates on startup while you investigate.
- Removed dependency on custom theme dictionaries so the app now relies on built-in WinUI resources for theming.

## How to verify
1. Clean and rebuild the app (packaged or unpackaged).
2. Launch the app. If the main XAML still fails to load, a fallback shell will appear instead of the process exiting. Check the log for the exact missing file (`Fatal exception during app launch (missing file during XAML load)` or `Fatal XAML parse during app launch`).
3. Confirm in build output (or the generated `.pri`) that all referenced XAML files are compiled Page resources.

## Maintenance tips
- When `EnableDefaultPageItems` is `false`, **every** XAML page or resource dictionary must be explicitly included with `<Page Include=...>`. Using `Update` only modifies existing items and will silently skip files that are not already included.
- If you add new resource dictionaries, add matching `<Page Include>` entries in the project file so they are compiled and discoverable at runtime.
- If the fallback shell appears, fix the missing XAML resource reported in the log, then rebuild to restore the full UI.
