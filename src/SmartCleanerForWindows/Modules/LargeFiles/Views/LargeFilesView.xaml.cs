using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SmartCleanerForWindows.Diagnostics;

namespace SmartCleanerForWindows.Modules.LargeFiles.Views;

public sealed partial class LargeFilesView
{
    public LargeFilesView()
    {
        InitializeComponent();
        UiConstructionLog.AttachFrameworkElementDiagnostics(this, "LargeFilesView");
        RegisterLayoutElements();
    }

    public event RoutedEventHandler? BrowseRequested;

    public event RoutedEventHandler? CancelRequested;

    public event RoutedEventHandler? ClearExclusionsRequested;

    public event TextChangedEventHandler? RootPathChanged;

    public event RoutedEventHandler? ScanRequested;

    public event RoutedEventHandler? DeleteRequested;

    public event RoutedEventHandler? ExcludeRequested;

    public event RoutedEventHandler? OpenRequested;

    public event RoutedEventHandler? RemoveExclusionRequested;

    private void OnLargeFilesBrowse(object sender, RoutedEventArgs e) => BrowseRequested?.Invoke(sender, e);

    private void OnLargeFilesCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);

    private void OnLargeFilesClearExclusions(object sender, RoutedEventArgs e) => ClearExclusionsRequested?.Invoke(sender, e);

    private void OnLargeFilesRootPathChanged(object sender, TextChangedEventArgs e) => RootPathChanged?.Invoke(sender, e);

    private void OnLargeFilesScan(object sender, RoutedEventArgs e) => ScanRequested?.Invoke(sender, e);

    private void OnLargeFileDelete(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(sender, e);

    private void OnLargeFileExclude(object sender, RoutedEventArgs e) => ExcludeRequested?.Invoke(sender, e);

    private void OnLargeFileOpen(object sender, RoutedEventArgs e) => OpenRequested?.Invoke(sender, e);

    private void OnLargeFilesRemoveExclusion(object sender, RoutedEventArgs e) => RemoveExclusionRequested?.Invoke(sender, e);

    private void RegisterLayoutElements()
    {
        _ = LargeFilesLayout;
        _ = LargeFilesOptionsColumn;
        _ = LargeFilesResultsPanel;
        _ = LargeFilesOptionsPanel;
    }
}
