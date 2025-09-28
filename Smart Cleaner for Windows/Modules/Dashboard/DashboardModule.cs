using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Modules.Dashboard;

public sealed class DashboardModule : INavigationModule
{
    public string Id => "dashboard";
    public string Title => "Dashboard";
    public Symbol Icon => Symbol.Home;
    public int Order => 0;
    public Type ViewType => typeof(DashboardView);
}
