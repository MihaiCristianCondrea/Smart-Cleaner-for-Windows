using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Modules.EmptyFolders;

public sealed class EmptyFoldersModule : INavigationModule
{
    public string Id => "empty-folders";
    public string Title => "Empty folders";
    public Symbol Icon => Symbol.Folder;
    public int Order => 1;
    public Type ViewType => typeof(EmptyFoldersView);
}
