# Empty Folder Cleaner

This repository contains a .NET 8 solution for scanning and deleting empty directories. The core logic is packaged as a reusable
library that powers both a cross-platform command line interface and a WinUI 3 desktop application for Windows.

## Project layout

```
EmptyFolderCleaner.sln
├── src/
│   ├── EmptyFolderCleaner.Core/       # Directory scanning and deletion logic
│   ├── EmptyFolderCleaner.Cli/        # System.CommandLine based CLI
│   └── EmptyFolderCleaner.WinUI/      # WinUI 3 desktop app (Windows App SDK)
└── tests/
    └── EmptyFolderCleaner.Core.Tests/ # xUnit tests for the core library
```

### Core library (`EmptyFolderCleaner.Core`)

The `DirectoryCleaner` type performs a bottom-up traversal of a directory tree and supports:

* Dry-run previews and destructive runs.
* Optional deletion to the Windows Recycle Bin (guarded at runtime on non-Windows platforms).
* Wildcard-based exclude patterns (`*` and `?`) applied to names or relative paths.
* Explicit exclude path list.
* Reparse point (symbolic link/junction) skipping by default.
* Depth limits and optional deletion of the root directory itself.
* Cancellation support and structured error reporting.

### Command line interface (`EmptyFolderCleaner.Cli`)

The CLI wraps the library with a friendly interface:

```bash
# Preview (default)
dotnet run --project src/EmptyFolderCleaner.Cli -- "C:/path/to/root"

# Permanently delete empty directories
dotnet run --project src/EmptyFolderCleaner.Cli -- "C:/path" --delete --permanent

# Delete using the Recycle Bin on Windows
dotnet run --project src/EmptyFolderCleaner.Cli -- "C:/path" --delete

# Apply exclusions and limit traversal depth
dotnet run --project src/EmptyFolderCleaner.Cli -- \
  "C:/path" --exclude ".git" --exclude "build/*" --depth 2 --json
```

Run `dotnet run --project src/EmptyFolderCleaner.Cli -- --help` for the full option list.

### WinUI 3 desktop application (`EmptyFolderCleaner.WinUI`)

An unpackaged WinUI 3 experience that ships with Fluent design defaults. It reuses the core library and
provides preview and delete flows, exclusion patterns, progress and cancellation.

Publishing a portable executable (on Windows with the Windows App SDK workload installed):

```powershell
dotnet publish src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj -c Release -p:PublishProfile=Win-x64-SelfContained
```

## Development

1. Install the .NET 8 SDK.
2. Restore dependencies and run the unit tests:

```bash
dotnet test tests/EmptyFolderCleaner.Core.Tests/EmptyFolderCleaner.Core.Tests.csproj
```

3. Run the CLI as shown above or open the solution in Visual Studio / Rider to work with the WinUI project.

> **Note:** Building the WinUI project requires Windows with the Windows App SDK workload. The CLI and tests run cross-platform.
