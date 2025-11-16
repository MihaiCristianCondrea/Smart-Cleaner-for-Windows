using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public sealed class RegistryDiskCleanupExecutor : IDiskCleanupExecutor
{
    private readonly IRegistryDiskCleanupInterop _interop;

    public RegistryDiskCleanupExecutor()
        : this(RegistryDiskCleanupInteropFacade.Instance)
    {
    }

    private RegistryDiskCleanupExecutor(IRegistryDiskCleanupInterop interop)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
    }

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

            using var root = _interop.TryOpenVolumeCaches(item.Descriptor.RegistryView);
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

    private HandlerExecutionResult RunHandler(
        RegistryKey handlerKey,
        DiskCleanupItem item,
        string drive,
        CancellationToken cancellationToken)
    {
        try
        {
            using var handler = _interop.TryCreateHandler(item.Descriptor.ClassId);
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

            _interop.FreeCoTaskMem(rawDisplay);
            _interop.FreeCoTaskMem(rawDescription);

            if (status < 0)
            {
                return HandlerExecutionResult.Failed(_interop.CreateErrorMessage(status));
            }

            var callback = _interop.CreateCallback(cancellationToken);
            var result = cache.Purge(0, callback);

            if (result == RegistryDiskCleanupInterop.HResults.OperationAborted && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (result < 0)
            {
                return HandlerExecutionResult.Failed(_interop.CreateErrorMessage(result));
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
