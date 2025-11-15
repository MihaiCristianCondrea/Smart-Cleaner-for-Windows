using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.InternetRepair.Views;

public sealed partial class InternetRepairView : UserControl
{
    public InternetRepairView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? RunRequested;

    public event RoutedEventHandler? CancelRequested;

    public event RoutedEventHandler? ActionSelectionChanged;

    private void OnInternetRepairRun(object sender, RoutedEventArgs e) => RunRequested?.Invoke(sender, e);

    private void OnInternetRepairCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);

    private void OnInternetRepairActionSelectionChanged(object sender, RoutedEventArgs e) =>
        ActionSelectionChanged?.Invoke(sender, e);
}
