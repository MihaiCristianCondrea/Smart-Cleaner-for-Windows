using System;
using System.IO;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public sealed class DiskCleanupVolumeService : IDiskCleanupVolumeService
{
    public string GetDefaultVolume()
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

    public string NormalizeVolume(string? volume)
    {
        var drive = string.IsNullOrWhiteSpace(volume) ? GetDefaultVolume() : volume.Trim();

        if (drive.Length == 2 && drive[1] == ':') // FIXME: Merge into pattern
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
}
