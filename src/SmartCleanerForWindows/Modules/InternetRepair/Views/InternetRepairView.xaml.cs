using Microsoft.UI.Xaml;

namespace SmartCleanerForWindows.Modules.InternetRepair.Views;

public sealed partial class InternetRepairView
{
    public InternetRepairView()
    {
        InitializeComponent();
        RegisterLayoutElements();
    }

    public event RoutedEventHandler? RunRequested;

    public event RoutedEventHandler? CancelRequested;

    public event RoutedEventHandler? ActionSelectionChanged;

    private void OnInternetRepairRun(object sender, RoutedEventArgs e) => RunRequested?.Invoke(sender, e);

    private void OnInternetRepairCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);

    private void OnInternetRepairActionSelectionChanged(object sender, RoutedEventArgs e) =>
        ActionSelectionChanged?.Invoke(sender, e);

    private void RegisterLayoutElements()
    {
        _ = InternetRepairLayout;
        _ = InternetRepairSecondaryColumn;
        _ = InternetRepairPrimaryPanel;
        _ = InternetRepairSecondaryPanel;
    }
}
