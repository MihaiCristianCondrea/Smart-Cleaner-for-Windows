using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core;

public sealed class RegistryDiskCleanupAnalyzer : IDiskCleanupAnalyzer
{
    public IReadOnlyList<DiskCleanupItem> Analyze(string drive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<DiskCleanupItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var root = RegistryDiskCleanupInterop.TryOpenVolumeCaches(view);
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

        var display = RegistryDiskCleanupInterop.ReadDisplayString(handlerKey, "Display");
        var description = RegistryDiskCleanupInterop.ReadDisplayString(handlerKey, "Description");

        try
        {
            using var handler = RegistryDiskCleanupInterop.TryCreateHandler(clsid);
            if (handler is null)
            {
                return null;
            }

            var cache = handler.Cache;
            var status = cache.Initialize(
                handlerKey.Handle.DangerousGetHandle(),
                drive,
                out var rawDisplay,
                out var rawDescription,
                out var flagsValue);

            try
            {
                var handlerDisplay = RegistryDiskCleanupInterop.PtrToString(rawDisplay);
                var handlerDescription = RegistryDiskCleanupInterop.PtrToString(rawDescription);

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
                var callback = new RegistryDiskCleanupInterop.DiskCleanupCallback(cancellationToken);
                var spaceStatus = cache.GetSpaceUsed(out size, callback);

                if (spaceStatus == RegistryDiskCleanupInterop.HResults.E_ABORT && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (spaceStatus < 0)
                {
                    error = RegistryDiskCleanupInterop.CreateErrorMessage(spaceStatus);
                    requiresElevation = spaceStatus == RegistryDiskCleanupInterop.HResults.E_ACCESSDENIED;
                    size = 0;
                }
            }
            else
            {
                error = RegistryDiskCleanupInterop.CreateErrorMessage(status);
                requiresElevation = status == RegistryDiskCleanupInterop.HResults.E_ACCESSDENIED;
            }

            var name = string.IsNullOrWhiteSpace(display) ? subKeyName : display;
            return new DiskCleanupItem(descriptor, name, description, size, flags, error, requiresElevation);
        }
        catch (COMException ex)
        {
            return new DiskCleanupItem(descriptor, display ?? subKeyName, description, 0, DiskCleanupFlags.None, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new DiskCleanupItem(descriptor, display ?? subKeyName, description, 0, DiskCleanupFlags.None, ex.Message, false);
        }
    }
}
