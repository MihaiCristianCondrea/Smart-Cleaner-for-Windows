using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Modules.Settings;

public sealed class SettingsModule : INavigationModule
{
    public string Id => "settings";
    public string Title => "Settings";
    public Symbol Icon => Symbol.Setting;
    public int Order => 100;
    public Type ViewType => typeof(SettingsView);
}
