using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public sealed class RegistryDiskCleanupExecutor : IDiskCleanupExecutor
{
    public DiskCleanupCleanResult Clean(string drive, IEnumerable<DiskCleanupItem> items, CancellationToken cancellationToken)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

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

            using var root = RegistryDiskCleanupInterop.TryOpenVolumeCaches(item.Descriptor.RegistryView);
            using var handlerKey = root?.OpenSubKey(item.Descriptor.SubKeyName);
            if (handlerKey is null)
            {
                failures.Add(new DiskCleanupFailure(item.Name, "The registry entry for this handler could not be opened."));
                continue;
            }

            var result = RunHandler(handlerKey, item, drive, cancellationToken);
            if (result.IsSuccess)
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
        try
        {
            using var handler = RegistryDiskCleanupInterop.TryCreateHandler(item.Descriptor.ClassId);
            if (handler is null)
            {
                return HandlerExecutionResult.Failed("The Disk Cleanup handler could not be instantiated.");
            }

            var cache = handler.Cache;
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
                return HandlerExecutionResult.Failed(RegistryDiskCleanupInterop.CreateErrorMessage(status));
            }

            var callback = new RegistryDiskCleanupInterop.DiskCleanupCallback(cancellationToken);
            var result = cache.Purge(0, callback);

            if (result == RegistryDiskCleanupInterop.HResults.E_ABORT && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (result < 0)
            {
                return HandlerExecutionResult.Failed(RegistryDiskCleanupInterop.CreateErrorMessage(result));
            }

            ulong freed = 0;
            var releaseStatus = cache.Deactivate(out var deactivateFlags);
            if (releaseStatus >= 0 && (deactivateFlags & 0x1) == 0)
            {
                cache.GetSpaceUsed(out freed, null);
            }

            return HandlerExecutionResult.CreateSuccess(freed);
        }
        catch (COMException ex)
        {
            return HandlerExecutionResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return HandlerExecutionResult.Failed(ex.Message);
        }
    }

    private sealed record HandlerExecutionResult(bool IsSuccess, ulong FreedSpace, string? ErrorMessage)
    {
        public static HandlerExecutionResult CreateSuccess(ulong freed) => new(true, freed, null);

        public static HandlerExecutionResult Failed(string? message) => new(false, 0, message);
    }
}
