using System;
using Microsoft.Win32;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public sealed class DiskCleanupItem
{
    public DiskCleanupItem()
        : this(
            new DiskCleanupHandlerDescriptor(Guid.Empty, string.Empty, RegistryView.Default),
            string.Empty,
            null,
            0,
            DiskCleanupFlags.None,
            null,
            false)
    {
    }

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

    public string Name { get; }

    public string? Description { get; }

    public ulong Size { get; }

    public DiskCleanupFlags Flags { get; }

    public string? Error { get; }

    public bool RequiresElevation { get; }

    public bool CanSelect => string.IsNullOrWhiteSpace(Error) && Size > 0;

    internal bool ShouldDisplay => !string.IsNullOrWhiteSpace(Error) || !Flags.HasFlag(DiskCleanupFlags.DontShowIfZero) || Size > 0;
}

internal sealed record DiskCleanupHandlerDescriptor(Guid ClassId, string SubKeyName, RegistryView RegistryView);
