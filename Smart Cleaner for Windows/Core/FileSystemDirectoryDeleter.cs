using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.VisualBasic.FileIO;

namespace Smart_Cleaner_for_Windows.Core;

public sealed class FileSystemDirectoryDeleter : IDirectoryDeleter
{
    public void Delete(string path, DirectoryDeletionMode mode)
    {
        switch (mode)
        {
            case DirectoryDeletionMode.Permanent:
                Directory.Delete(path, recursive: false);
                break;
            case DirectoryDeletionMode.RecycleBin:
                DeleteToRecycleBin(path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported deletion mode.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DeleteToRecycleBin(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Deleting to the Recycle Bin is only supported on Windows.");
        }

        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }
}
