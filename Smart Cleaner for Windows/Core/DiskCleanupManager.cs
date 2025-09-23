using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Provides helpers to surface the built-in Windows Disk Cleanup handlers.
/// </summary>
public static class DiskCleanupManager
{
    private const string VolumeCachesPath = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches";

    /// <summary>
    /// Gets the default volume that should be analyzed (the system drive).
    /// </summary>
    /// <returns>The root path for the system drive.</returns>
    public static string GetDefaultVolume()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "C:\\";
        }

        try
        {
            var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrWhiteSpace(windowsPath))
            {
                windowsPath = Environment.SystemDirectory;
            }

            if (!string.IsNullOrWhiteSpace(windowsPath))
            {
                var root = Path.GetPathRoot(windowsPath);
                if (!string.IsNullOrWhiteSpace(root))
                {
                    return EnsureTrailingBackslash(root);
                }
            }
        }
        catch
        {
            // Ignore lookup failures and fall back to the default drive.
        }

        return "C:\\";
    }

    /// <summary>
    /// Enumerates the registered Disk Cleanup handlers and returns their estimated savings.
    /// </summary>
    /// <param name="volume">The drive letter (e.g. C:\) that should be analyzed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The list of handlers and their associated metadata.</returns>
    public static Task<IReadOnlyList<DiskCleanupItem>> AnalyzeAsync(string? volume, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Disk Cleanup integration is only available on Windows.");
        }

        var drive = NormalizeVolume(volume);
        return RunStaAsync(ct => AnalyzeInternal(drive, ct), cancellationToken);
    }

    /// <summary>
    /// Requests the selected Disk Cleanup handlers to delete their unneeded data.
    /// </summary>
    /// <param name="volume">The drive letter (e.g. C:\) that should be cleaned.</param>
    /// <param name="items">The handlers that should be executed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A summary of the clean operation.</returns>
    public static Task<DiskCleanupCleanResult> CleanAsync(
        string? volume,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Disk Cleanup integration is only available on Windows.");
        }

        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var drive = NormalizeVolume(volume);
        return RunStaAsync(ct => CleanInternal(drive, items, ct), cancellationToken);
    }

    private static string NormalizeVolume(string? volume)
    {
        var drive = string.IsNullOrWhiteSpace(volume) ? GetDefaultVolume() : volume.Trim();

        if (drive.Length == 2 && drive[1] == ':')
        {
            drive += '\\';
        }

        if (!drive.EndsWith("\\", StringComparison.Ordinal))
        {
            try
            {
                drive = EnsureTrailingBackslash(Path.GetFullPath(drive));
            }
            catch
            {
                drive = EnsureTrailingBackslash(drive);
            }
        }

        return drive;
    }

    private static string EnsureTrailingBackslash(string path)
    {
        if (!path.EndsWith("\\", StringComparison.Ordinal))
        {
            path += '\\';
        }

        return path;
    }

    private static Task<T> RunStaAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.ThrowIfCancellationRequested();

        var thread = new Thread(() =>
        {
            try
            {
                var hr = NativeMethods.CoInitializeEx(IntPtr.Zero, CoInit.APARTMENTTHREADED);
                var initialized = hr >= 0;

                if (hr == HResults.RPC_E_CHANGED_MODE)
                {
                    // If the apartment was already initialized differently we fall back to MTA.
                    hr = NativeMethods.CoInitializeEx(IntPtr.Zero, CoInit.MULTITHREADED);
                    initialized = hr >= 0;
                }

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = action(cancellationToken);
                    tcs.TrySetResult(result);
                }
                finally
                {
                    if (initialized)
                    {
                        NativeMethods.CoUninitialize();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "DiskCleanupWorker"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return tcs.Task;
    }

    private static IReadOnlyList<DiskCleanupItem> AnalyzeInternal(string drive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<DiskCleanupItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var root = TryOpenVolumeCaches(view);
            if (root is null)
            {
                continue;
            }

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyId = $"{view}:{subKeyName}";
                if (!seen.Add(keyId))
                {
                    continue;
                }

                using var handlerKey = root.OpenSubKey(subKeyName);
                if (handlerKey is null)
                {
                    continue;
                }

                var item = AnalyzeHandler(handlerKey, subKeyName, view, drive, cancellationToken);
                if (item is not null && item.ShouldDisplay)
                {
                    results.Add(item);
                }
            }
        }

        results.Sort((left, right) =>
        {
            var bySize = right.Size.CompareTo(left.Size);
            if (bySize != 0)
            {
                return bySize;
            }

            return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        return results;
    }

    private static DiskCleanupItem? AnalyzeHandler(
        RegistryKey handlerKey,
        string subKeyName,
        RegistryView view,
        string drive,
        CancellationToken cancellationToken)
    {
        var clsidValue = handlerKey.GetValue("CLSID") as string;
        if (string.IsNullOrWhiteSpace(clsidValue) || !Guid.TryParse(clsidValue, out var clsid))
        {
            return null;
        }

        var descriptor = new DiskCleanupHandlerDescriptor(clsid, subKeyName, view);

        var display = ReadDisplayString(handlerKey, "Display");
        var description = ReadDisplayString(handlerKey, "Description");

        object? comObject = null;
        try
        {
            var type = Type.GetTypeFromCLSID(clsid, throwOnError: false);
            if (type is null)
            {
                return null;
            }

            comObject = Activator.CreateInstance(type);
            if (comObject is not IEmptyVolumeCache cache)
            {
                return null;
            }

            var status = cache.Initialize(
                handlerKey.Handle.DangerousGetHandle(),
                drive,
                out var rawDisplay,
                out var rawDescription,
                out var flagsValue);

            try
            {
                var handlerDisplay = PtrToString(rawDisplay);
                var handlerDescription = PtrToString(rawDescription);

                display ??= handlerDisplay;
                description ??= handlerDescription;
            }
            finally
            {
                if (rawDisplay != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(rawDisplay);
                }

                if (rawDescription != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(rawDescription);
                }
            }

            var flags = (DiskCleanupFlags)flagsValue;

            if (flags.HasFlag(DiskCleanupFlags.RemoveFromList))
            {
                return null;
            }

            string? error = null;
            var requiresElevation = false;
            ulong size = 0;

            if (status >= 0)
            {
                var callback = new DiskCleanupCallback(cancellationToken);
                var spaceStatus = cache.GetSpaceUsed(out size, callback);

                if (spaceStatus == HResults.E_ABORT && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (spaceStatus < 0)
                {
                    error = CreateErrorMessage(spaceStatus);
                    requiresElevation = spaceStatus == HResults.E_ACCESSDENIED;
                    size = 0;
                }
            }
            else
            {
                error = CreateErrorMessage(status);
                requiresElevation = status == HResults.E_ACCESSDENIED;
            }

            var name = string.IsNullOrWhiteSpace(display) ? subKeyName : display;
            var item = new DiskCleanupItem(descriptor, name, description, size, flags, error, requiresElevation);
            return item;
        }
        catch (COMException ex)
        {
            return new DiskCleanupItem(descriptor, display ?? subKeyName, description, 0, DiskCleanupFlags.None, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new DiskCleanupItem(descriptor, display ?? subKeyName, description, 0, DiskCleanupFlags.None, ex.Message, false);
        }
        finally
        {
            if (comObject is not null)
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }
    }

    private static DiskCleanupCleanResult CleanInternal(
        string drive,
        IEnumerable<DiskCleanupItem> items,
        CancellationToken cancellationToken)
    {
        var failures = new List<DiskCleanupFailure>();
        ulong freedTotal = 0;
        var successCount = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.CanSelect)
            {
                continue;
            }

            using var root = TryOpenVolumeCaches(item.Descriptor.RegistryView);
            using var handlerKey = root?.OpenSubKey(item.Descriptor.SubKeyName);
            if (handlerKey is null)
            {
                failures.Add(new DiskCleanupFailure(item.Name, "The registry entry for this handler could not be opened."));
                continue;
            }

            var result = RunHandler(handlerKey, item, drive, cancellationToken);
            if (result.Success)
            {
                successCount++;
                freedTotal += result.FreedSpace;
            }
            else if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                failures.Add(new DiskCleanupFailure(item.Name, result.ErrorMessage!));
            }
        }

        return new DiskCleanupCleanResult(freedTotal, successCount, failures);
    }

    private static HandlerExecutionResult RunHandler(
        RegistryKey handlerKey,
        DiskCleanupItem item,
        string drive,
        CancellationToken cancellationToken)
    {
        object? comObject = null;
        try
        {
            var type = Type.GetTypeFromCLSID(item.Descriptor.ClassId, throwOnError: false);
            if (type is null)
            {
                return HandlerExecutionResult.Failed("The Disk Cleanup handler could not be instantiated.");
            }

            comObject = Activator.CreateInstance(type);
            if (comObject is not IEmptyVolumeCache cache)
            {
                return HandlerExecutionResult.Failed("The Disk Cleanup handler does not implement the expected interface.");
            }

            var status = cache.Initialize(
                handlerKey.Handle.DangerousGetHandle(),
                drive,
                out var rawDisplay,
                out var rawDescription,
                out _);

            if (rawDisplay != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(rawDisplay);
            }

            if (rawDescription != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(rawDescription);
            }

            if (status < 0)
            {
                var message = CreateErrorMessage(status);
                return HandlerExecutionResult.Failed(message);
            }

            var callback = new DiskCleanupCallback(cancellationToken);

            var beforeStatus = cache.GetSpaceUsed(out var before, callback);
            if (beforeStatus == HResults.E_ABORT && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (beforeStatus < 0)
            {
                return HandlerExecutionResult.Failed(CreateErrorMessage(beforeStatus));
            }

            var purgeStatus = cache.Purge(before, callback);
            if (purgeStatus == HResults.E_ABORT && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (purgeStatus < 0)
            {
                return HandlerExecutionResult.Failed(CreateErrorMessage(purgeStatus));
            }

            var afterStatus = cache.GetSpaceUsed(out var after, callback);
            if (afterStatus == HResults.E_ABORT && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            var freed = beforeStatus >= 0 && afterStatus >= 0 && before >= after ? before - after : 0;
            cache.Deactivate(out _);
            return HandlerExecutionResult.Success(freed);
        }
        catch (COMException ex)
        {
            return HandlerExecutionResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return HandlerExecutionResult.Failed(ex.Message);
        }
        finally
        {
            if (comObject is not null)
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }
    }

    private static RegistryKey? TryOpenVolumeCaches(RegistryView view)
    {
        try
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            return baseKey.OpenSubKey(VolumeCachesPath);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadDisplayString(RegistryKey key, string valueName)
    {
        if (key.GetValue(valueName) is not string value || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ExpandResourceString(value);
    }

    private static string? ExpandResourceString(string value)
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

    private static string? PtrToString(IntPtr value)
    {
        if (value == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUni(value);
    }

    private static string CreateErrorMessage(int hresult)
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

    private sealed record HandlerExecutionResult(bool Success, ulong FreedSpace, string? ErrorMessage)
    {
        public static HandlerExecutionResult Success(ulong freed) => new(true, freed, null);

        public static HandlerExecutionResult Failed(string? message) => new(false, 0, message);
    }

    private sealed class DiskCleanupCallback : IEmptyVolumeCacheCallback
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

    private static class HResults
    {
        public const int S_OK = 0;
        public const int E_ABORT = unchecked((int)0x80004004);
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);
        public const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);
    }

    [Flags]
    public enum DiskCleanupFlags : uint
    {
        None = 0,
        HasSettings = 0x00000001,
        EnableByDefault = 0x00000002,
        RemoveFromList = 0x00000004,
        DontShowIfZero = 0x00000008,
        SettingsMode = 0x00000020,
        OutOfDiskSpace = 0x00000040,
        UserConsentObtained = 0x00000080,
        SystemAutorun = 0x00000100,
        RunByDefault = 0x00000200,
    }

    internal sealed record DiskCleanupHandlerDescriptor(Guid ClassId, string SubKeyName, RegistryView RegistryView);

    /// <summary>
    /// Represents a Disk Cleanup handler and the metadata retrieved from the system.
    /// </summary>
    public sealed class DiskCleanupItem
    {
        internal DiskCleanupItem(
            DiskCleanupHandlerDescriptor descriptor,
            string name,
            string? description,
            ulong size,
            DiskCleanupFlags flags,
            string? error,
            bool requiresElevation)
        {
            Descriptor = descriptor;
            Name = name;
            Description = description;
            Size = size;
            Flags = flags;
            Error = error;
            RequiresElevation = requiresElevation;
        }

        internal DiskCleanupHandlerDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the display name reported by the handler or registry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the optional description of the handler.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the estimated amount of reclaimable space, in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Gets the flags reported by the handler.
        /// </summary>
        public DiskCleanupFlags Flags { get; }

        /// <summary>
        /// Gets the error message, if any, returned by the handler.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Gets a value indicating whether the handler requires elevated privileges.
        /// </summary>
        public bool RequiresElevation { get; }

        /// <summary>
        /// Gets a value indicating whether the handler can be selected by the user.
        /// </summary>
        public bool CanSelect => string.IsNullOrWhiteSpace(Error) && Size > 0;

        internal bool ShouldDisplay => !string.IsNullOrWhiteSpace(Error) || !Flags.HasFlag(DiskCleanupFlags.DontShowIfZero) || Size > 0;
    }

    /// <summary>
    /// Represents the outcome of a Disk Cleanup purge request.
    /// </summary>
    public sealed class DiskCleanupCleanResult
    {
        internal DiskCleanupCleanResult(ulong freed, int successCount, IReadOnlyList<DiskCleanupFailure> failures)
        {
            Freed = freed;
            SuccessCount = successCount;
            Failures = failures;
        }

        /// <summary>
        /// Gets the number of bytes reported as freed by the handlers.
        /// </summary>
        public ulong Freed { get; }

        /// <summary>
        /// Gets the number of handlers that reported success.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets the collection of failures reported by individual handlers.
        /// </summary>
        public IReadOnlyList<DiskCleanupFailure> Failures { get; }

        /// <summary>
        /// Gets a value indicating whether any handler reported a failure.
        /// </summary>
        public bool HasFailures => Failures.Count > 0;
    }

    /// <summary>
    /// Represents a failure encountered while running a Disk Cleanup handler.
    /// </summary>
    public sealed record DiskCleanupFailure(string Name, string Message);

    private static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern int CoInitializeEx(IntPtr pvReserved, CoInit dwCoInit);

        [DllImport("ole32.dll")]
        public static extern void CoUninitialize();

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
    private interface IEmptyVolumeCache
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
    private interface IEmptyVolumeCacheCallback
    {
        [PreserveSig]
        int ScanProgress(ulong dwlSpaceUsed, ulong dwlSpaceTotal);

        [PreserveSig]
        int PurgeProgress(ulong dwlSpaceFreed, ulong dwlSpaceToFree);

        [PreserveSig]
        int ScanCompleted();
    }

    [Flags]
    private enum CoInit : uint
    {
        MULTITHREADED = 0x0,
        APARTMENTTHREADED = 0x2,
    }
}

