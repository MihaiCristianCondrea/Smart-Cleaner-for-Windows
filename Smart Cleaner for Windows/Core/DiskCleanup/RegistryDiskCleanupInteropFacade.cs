using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core.DiskCleanup;

internal sealed class RegistryDiskCleanupInteropFacade : IRegistryDiskCleanupInterop
{
    public static RegistryDiskCleanupInteropFacade Instance { get; } = new();

    private RegistryDiskCleanupInteropFacade()
    {
    }

    public RegistryKey? TryOpenVolumeCaches(RegistryView view)
        => RegistryDiskCleanupInterop.TryOpenVolumeCaches(view);

    public RegistryDiskCleanupInterop.DiskCleanupHandler? TryCreateHandler(Guid clsid)
        => RegistryDiskCleanupInterop.TryCreateHandler(clsid);

    public string? ReadDisplayString(RegistryKey key, string valueName)
        => RegistryDiskCleanupInterop.ReadDisplayString(key, valueName);

    public string? PtrToString(IntPtr value)
        => RegistryDiskCleanupInterop.PtrToString(value);

    public string CreateErrorMessage(int hresult)
        => RegistryDiskCleanupInterop.CreateErrorMessage(hresult);

    public void FreeCoTaskMem(IntPtr pointer)
    {
        if (pointer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    public RegistryDiskCleanupInterop.DiskCleanupCallback CreateCallback(CancellationToken token)
        => new(token);
}
