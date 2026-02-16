# Windows packaging, deployment, and process model (project notes)

This note summarizes how the official Windows guidance applies to this repository.

## 1) Packaged vs unpackaged

For this app, the recommended/default path is **packaged** (MSIX) because it provides:

- package identity at runtime,
- cleaner install/uninstall/update behavior,
- easier enterprise/store distribution.

Unpackaged builds are still useful for local development/debugging, but they do **not** provide package identity.

## 2) Why package identity matters

Windows features such as notifications and some extensibility points depend on package identity. Packaged apps have it; unpackaged apps do not.

Because Smart Cleaner uses WinUI + Windows App SDK and is already configured for single-project MSIX, packaging remains the primary deployment model.

## 3) Deployment/distribution model used here

- Local dev: `dotnet build` / `dotnet run`.
- Package generation: MSIX via single-project tooling.
- Recommended automation flag for packaging: `/p:GenerateAppxPackageOnBuild=true`.

## 4) Process model (AppContainer vs Medium IL)

Desktop apps can run as AppContainer or Medium IL depending on manifest/project configuration.

Current project intent is standard desktop WinUI behavior with MSIX packaging. If process isolation requirements change later, evaluate AppContainer configuration as a separate hardening task.

## 5) Windows App SDK deployment mode

Windows App SDK apps can be:

- **Framework-dependent** (default for many setups), or
- **Self-contained** (dependencies carried with the app).

This project already uses self-contained publish profiles for architecture-specific publishing; keep using the provided publish profiles unless there is a deliberate deployment strategy change.

## 6) Build commands (quick reference)

### Build only

```powershell
dotnet restore
dotnet build "Smart Cleaner for Windows.sln" -c Debug
```

### Build and generate MSIX

```powershell
dotnet msbuild "src/SmartCleanerForWindows/SmartCleanerForWindows.csproj" `
  /t:Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:GenerateAppxPackageOnBuild=true
```

### Publish profile output (self-contained)

```powershell
dotnet publish "src/SmartCleanerForWindows/SmartCleanerForWindows.csproj" -c Release -p:PublishProfile=win-x64
```

## 7) When to choose a packaging project instead

Single-project MSIX is ideal for this app's current shape. If future requirements need multiple executables combined into one package, switch to a dedicated packaging-project workflow.
