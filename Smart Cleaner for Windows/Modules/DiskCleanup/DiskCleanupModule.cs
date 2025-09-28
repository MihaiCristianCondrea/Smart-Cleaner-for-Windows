using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Modules.DiskCleanup;

public sealed class DiskCleanupModule : INavigationModule
{
    public string Id => "disk-cleanup";
    public string Title => "Disk cleanup";
    public Symbol Icon => Symbol.Delete;
    public int Order => 3;
    public Type ViewType => typeof(DiskCleanupView);
}
