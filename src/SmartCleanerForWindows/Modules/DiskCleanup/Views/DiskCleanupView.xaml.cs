using Microsoft.UI.Xaml;

using SmartCleanerForWindows.Diagnostics;

namespace SmartCleanerForWindows.Modules.DiskCleanup.Views;

public sealed partial class DiskCleanupView
{
    public DiskCleanupView()
    {
        InitializeComponent();
        UiConstructionLog.AttachFrameworkElementDiagnostics(this, "DiskCleanupView");
    }

    public event RoutedEventHandler? AnalyzeRequested;

    public event RoutedEventHandler? CleanRequested;

    public event RoutedEventHandler? CancelRequested;

    private void OnDiskCleanupAnalyze(object sender, RoutedEventArgs e) => AnalyzeRequested?.Invoke(sender, e);

    private void OnDiskCleanupClean(object sender, RoutedEventArgs e) => CleanRequested?.Invoke(sender, e);

    private void OnCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);
}
