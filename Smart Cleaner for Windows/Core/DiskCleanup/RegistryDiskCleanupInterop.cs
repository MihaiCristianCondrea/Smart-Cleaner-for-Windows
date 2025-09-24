using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core;

internal static class RegistryDiskCleanupInterop
{
    public const string VolumeCachesPath = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches";

    public static RegistryKey? TryOpenVolumeCaches(RegistryView view)
    {
        try
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(VolumeCachesPath);
        }
        catch
        {
            return null;
        }
    }

    public static DiskCleanupHandler? TryCreateHandler(Guid clsid)
    {
        var type = Type.GetTypeFromCLSID(clsid, throwOnError: false);
        if (type is null)
        {
            return null;
        }

        var comObject = Activator.CreateInstance(type);
        if (comObject is not IEmptyVolumeCache cache)
        {
            if (comObject is not null)
            {
                Marshal.FinalReleaseComObject(comObject);
            }

            return null;
        }

        return new DiskCleanupHandler(cache, comObject);
    }

    public static string? ReadDisplayString(RegistryKey key, string valueName)
    {
        if (key.GetValue(valueName) is not string value || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ExpandResourceString(value);
    }

    public static string? ExpandResourceString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value[0] == '@')
        {
            var buffer = new StringBuilder(1024);
            var hr = NativeMethods.SHLoadIndirectString(value, buffer, (uint)buffer.Capacity, IntPtr.Zero);
            if (hr >= 0)
            {
                return buffer.ToString();
            }
        }

        return Environment.ExpandEnvironmentVariables(value);
    }

    public static string? PtrToString(IntPtr value)
    {
        if (value == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUni(value);
    }

    public static string CreateErrorMessage(int hresult)
    {
        try
        {
            var exception = Marshal.GetExceptionForHR(hresult);
            if (exception is not null && !string.IsNullOrWhiteSpace(exception.Message))
            {
                return exception.Message;
            }
        }
        catch
        {
            // Ignore failure to materialize an exception for this HRESULT.
        }

        return $"HRESULT 0x{hresult:X8}";
    }

    internal sealed class DiskCleanupHandler : IDisposable
    {
        private readonly object _comObject;
        private bool _disposed;

        public DiskCleanupHandler(IEmptyVolumeCache cache, object comObject)
        {
            Cache = cache;
            _comObject = comObject;
        }

        public IEmptyVolumeCache Cache { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Marshal.FinalReleaseComObject(_comObject);
        }
    }

    internal sealed class DiskCleanupCallback : IEmptyVolumeCacheCallback
    {
        private readonly CancellationToken _token;

        public DiskCleanupCallback(CancellationToken token) => _token = token;

        public int ScanProgress(ulong dwlSpaceUsed, ulong dwlSpaceTotal)
        {
            return _token.IsCancellationRequested ? HResults.E_ABORT : HResults.S_OK;
        }

        public int PurgeProgress(ulong dwlSpaceFreed, ulong dwlSpaceToFree)
        {
            return _token.IsCancellationRequested ? HResults.E_ABORT : HResults.S_OK;
        }

        public int ScanCompleted()
        {
            return _token.IsCancellationRequested ? HResults.E_ABORT : HResults.S_OK;
        }
    }

    internal static class HResults
    {
        public const int S_OK = 0;
        public const int E_ABORT = unchecked((int)0x80004004);
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);
    }

    private static class NativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int SHLoadIndirectString(
            string pszSource,
            StringBuilder pszOutBuf,
            uint cchOutBuf,
            IntPtr ppvReserved);
    }

    [ComImport]
    [Guid("8FCE5227-04DA-11D1-A004-00805F8ABE06")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEmptyVolumeCache
    {
        [PreserveSig]
        int Initialize(
            IntPtr hkRegKey,
            [MarshalAs(UnmanagedType.LPWStr)] string pcwszVolume,
            out IntPtr ppwszDisplayName,
            out IntPtr ppwszDescription,
            out uint pdwFlags);

        [PreserveSig]
        int GetSpaceUsed(out ulong pdwlSpaceUsed, [MarshalAs(UnmanagedType.Interface)] IEmptyVolumeCacheCallback? picb);

        [PreserveSig]
        int Purge(ulong dwSpaceToFree, [MarshalAs(UnmanagedType.Interface)] IEmptyVolumeCacheCallback? picb);

        [PreserveSig]
        int ShowProperties(IntPtr hwndOwner);

        [PreserveSig]
        int Deactivate(out uint pdwFlags);
    }

    [ComImport]
    [Guid("6E793361-73C6-11D0-8469-00AA00442901")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEmptyVolumeCacheCallback
    {
        [PreserveSig]
        int ScanProgress(ulong dwlSpaceUsed, ulong dwlSpaceTotal);

        [PreserveSig]
        int PurgeProgress(ulong dwlSpaceFreed, ulong dwlSpaceToFree);

        [PreserveSig]
        int ScanCompleted();
    }
}
