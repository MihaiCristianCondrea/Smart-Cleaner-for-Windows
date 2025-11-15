# WinUI XAML compiler troubleshooting

When `dotnet build` or Visual Studio reports `XamlCompiler.exe exited with code 1`, the WinUI build step failed before the C# compiler ran. The message itself is generic—the real error is buried in the XAML compiler output. Use the steps below to capture the diagnostics and fix the offending markup.

## Run the compiler manually from PowerShell

1. Open **Windows PowerShell**.
2. Change to the WinUI project directory:
   ```powershell
   cd "C:\Users\<you>\source\repos\MihaiCristianCondrea\Smart-Cleaner-for-Windows\src\SmartCleanerForWindows"
   ```
3. Invoke the compiler with `&` (PowerShell's call operator) and PowerShell backticks for optional line continuation:
   ```powershell
   & "C:\Users\<you>\.nuget\packages\microsoft.windowsappsdk.winui\1.8.250906003\tools\net472\XamlCompiler.exe" `
     "obj\x64\Debug\net9.0-windows10.0.19041.0\win-x64\input.json" `
     "obj\x64\Debug\net9.0-windows10.0.19041.0\win-x64\output.json"
   ```

   *Do not* use CMD-style carets (`^`) in PowerShell; they are treated as literal characters and nothing executes. If you omit the leading `&`, PowerShell will also treat the command path as plain text instead of launching the compiler.

4. Alternatively, run everything on one line:
   ```powershell
   & "C:\Users\<you>\.nuget\packages\microsoft.windowsappsdk.winui\1.8.250906003\tools\net472\XamlCompiler.exe" "obj\x64\Debug\net9.0-windows10.0.19041.0\win-x64\input.json" "obj\x64\Debug\net9.0-windows10.0.19041.0\win-x64\output.json"
   ```

The compiler will now print the real diagnostics instead of failing silently.

## Inspect the generated output

The WinUI tool serialises its results to `obj\x64\Debug\net9.0-windows10.0.19041.0\win-x64\output.json`. Open the file in your editor and look for entries with:

- `"errorCode"` and `"errorMessage"`
- `"file"`, `"lineNumber"`, and `"columnNumber"`

These values pinpoint the XAML file and the location that triggered the failure. The console output often repeats the same information.

## Common causes of `exit code 1`

Once you know the failing file and line, check for the usual culprits:

- **Typos in element names** – e.g. `<Gridl>` instead of `<Grid>`.
- **Missing or incorrect XML namespaces** for custom controls.
- **Resource keys that no longer exist**, such as `{StaticResource MissingBrush}`.
- **Event handler mismatches** between the XAML and its code-behind.
- **Partial class mismatches** where the XAML `x:Class` does not match the namespace or class name in the `.xaml.cs` file.

Fix the markup, save, and rerun `dotnet build`.

## Verification checklist

- [ ] `dotnet build` (or Visual Studio) completes without `XamlCompiler.exe` errors.
- [ ] Updated XAML renders correctly in the running application.
- [ ] No additional XAML warnings appear in the PowerShell output or `output.json`.
