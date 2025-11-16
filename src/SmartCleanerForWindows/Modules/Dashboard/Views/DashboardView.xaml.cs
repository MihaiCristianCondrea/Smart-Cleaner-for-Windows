using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.Modules.Dashboard.Views;

public sealed partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    public TextBlock StorageSummaryTextBlock => StorageSummaryText;

    public TextBlock StorageTipTextBlock => StorageTipText;

    public ItemsControl DriveUsageListControl => DriveUsageList;

    public event EventHandler? NavigateToEmptyFoldersRequested;

    public event EventHandler? NavigateToLargeFilesRequested;

    public event EventHandler? NavigateToDiskCleanupRequested;

    public event EventHandler? NavigateToInternetRepairRequested;

    private void OnNavigateToEmptyFolders(object sender, RoutedEventArgs e) =>
        NavigateToEmptyFoldersRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToLargeFiles(object sender, RoutedEventArgs e) =>
        NavigateToLargeFilesRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToDiskCleanup(object sender, RoutedEventArgs e) =>
        NavigateToDiskCleanupRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToInternetRepair(object sender, RoutedEventArgs e) =>
        NavigateToInternetRepairRequested?.Invoke(this, EventArgs.Empty);
}
