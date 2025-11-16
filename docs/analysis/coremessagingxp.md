# CoreMessagingXP.dll crash research

* Windows App SDK / WinUI 3 packaged apps still run inside a Win32 host that pumps messages through CoreMessagingXP.dll. When the managed entry point crashes before a window is shown, the native host tears down the process and Event Viewer attributes the fault to CoreMessagingXP.dll even though the real problem is a .NET exception. 
* HRESULT `0xC0000602` maps to `STATUS_FAIL_FAST_EXCEPTION`, which the CLR uses when it terminates immediately after an unhandled managed exception (or after calling `Environment.FailFast`). You only see the native CoreMessagingXP.dll fault because the fail-fast bypasses WinUI's normal exception dialogs. 
* Therefore, the fix is to capture exceptions as early as possible (Program.Main/App constructors) and flush them to disk before the host tears down the process, so we can see the real managed stack trace that is causing the startup crash.
