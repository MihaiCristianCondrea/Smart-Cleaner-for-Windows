using System;
using System.Threading;
using Microsoft.Win32;

namespace SmartCleanerForWindows.Core.DiskCleanup;

internal interface IRegistryDiskCleanupInterop
{
    RegistryKey? TryOpenVolumeCaches(RegistryView view);

    RegistryDiskCleanupInterop.DiskCleanupHandler? TryCreateHandler(Guid clsid);

    string? ReadDisplayString(RegistryKey key, string valueName);

    string? PtrToString(IntPtr value);

    string CreateErrorMessage(int hresult);

    void FreeCoTaskMem(IntPtr pointer);

    RegistryDiskCleanupInterop.DiskCleanupCallback CreateCallback(CancellationToken token);
}
