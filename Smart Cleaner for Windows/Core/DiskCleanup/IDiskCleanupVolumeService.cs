namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public interface IDiskCleanupVolumeService
{
    string GetDefaultVolume();

    string NormalizeVolume(string? volume);
}
