using System;

namespace SmartCleanerForWindows.Core.DiskCleanup;

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
