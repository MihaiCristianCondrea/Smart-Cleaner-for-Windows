using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.DiskCleanup.Views;

public sealed partial class DiskCleanupView : UserControl
{
    public DiskCleanupView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? AnalyzeRequested;

    public event RoutedEventHandler? CleanRequested;

    public event RoutedEventHandler? CancelRequested;

    private void OnDiskCleanupAnalyze(object sender, RoutedEventArgs e) => AnalyzeRequested?.Invoke(sender, e);

    private void OnDiskCleanupClean(object sender, RoutedEventArgs e) => CleanRequested?.Invoke(sender, e);

    private void OnCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);
}
