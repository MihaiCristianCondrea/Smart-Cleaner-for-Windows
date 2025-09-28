using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.Dashboard;

public sealed partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
