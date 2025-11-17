using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace SmartCleanerForWindows.Modules.EmptyFolders.Views;

public sealed partial class EmptyFoldersView
{
    public EmptyFoldersView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? BrowseRequested;

    public event RoutedEventHandler? CancelRequested;

    public event TypedEventHandler<TreeView, TreeViewSelectionChangedEventArgs>? CandidatesSelectionChanged;

    public event RoutedEventHandler? InlineExclusionsCleared;

    public event RoutedEventHandler? ResultFiltersCleared;

    public event RoutedEventHandler? DeleteRequested;

    public event RoutedEventHandler? ExcludeSelectedRequested;

    public event RoutedEventHandler? IncludeSelectedRequested;

    public event RoutedEventHandler? PreviewRequested;

    public event TextChangedEventHandler? ResultSearchChanged;

    public event SelectionChangedEventHandler? ResultSortChanged;

    public event RoutedEventHandler? HideExcludedToggled;

    public event TextChangedEventHandler? RootPathTextChanged;

    private void OnBrowse(object sender, RoutedEventArgs e) => BrowseRequested?.Invoke(sender, e);

    private void OnCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);

    private void OnCandidatesSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args) =>
        CandidatesSelectionChanged?.Invoke(sender, args);

    private void OnHideExcludedToggled(object sender, RoutedEventArgs e) => HideExcludedToggled?.Invoke(sender, e);

    private void OnClearInlineExclusions(object sender, RoutedEventArgs e) => InlineExclusionsCleared?.Invoke(sender, e);

    private void OnClearResultFilters(object sender, RoutedEventArgs e) => ResultFiltersCleared?.Invoke(sender, e);

    private void OnDelete(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(sender, e);

    private void OnExcludeSelected(object sender, RoutedEventArgs e) => ExcludeSelectedRequested?.Invoke(sender, e);

    private void OnIncludeSelected(object sender, RoutedEventArgs e) => IncludeSelectedRequested?.Invoke(sender, e);

    private void OnPreview(object sender, RoutedEventArgs e) => PreviewRequested?.Invoke(sender, e);

    private void OnResultSearchChanged(object sender, TextChangedEventArgs e) => ResultSearchChanged?.Invoke(sender, e);

    private void OnResultSortChanged(object sender, SelectionChangedEventArgs e) => ResultSortChanged?.Invoke(sender, e);

    private void RootPathBox_TextChanged(object sender, TextChangedEventArgs e) => RootPathTextChanged?.Invoke(sender, e);
}
