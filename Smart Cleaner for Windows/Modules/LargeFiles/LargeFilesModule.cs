using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Modules.LargeFiles;

public sealed class LargeFilesModule : INavigationModule
{
    public string Id => "large-files";
    public string Title => "Large files";
    public Symbol Icon => Symbol.SaveLocal;
    public int Order => 2;
    public Type ViewType => typeof(LargeFilesView);
}
