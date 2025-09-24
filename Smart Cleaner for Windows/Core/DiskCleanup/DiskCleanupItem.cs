using System;
using Microsoft.Win32;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

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
