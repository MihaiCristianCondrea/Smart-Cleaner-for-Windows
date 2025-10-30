using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.EmptyFolders.Views;

public sealed partial class EmptyFoldersView : UserControl
{
    public EmptyFoldersView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? BrowseRequested;

    public event RoutedEventHandler? CancelRequested;

    public event TreeViewSelectionChangedEventHandler? CandidatesSelectionChanged; // FIXME: Cannot resolve symbol 'TreeViewSelectionChangedEventHandler'

    public event RoutedEventHandler? InlineExclusionsCleared;

    public event RoutedEventHandler? ResultFiltersCleared;

    public event RoutedEventHandler? DeleteRequested;

    public event RoutedEventHandler? ExcludeSelectedRequested;

    public event RoutedEventHandler? IncludeSelectedRequested;

    public event RoutedEventHandler? PreviewRequested;

    public event TextChangedEventHandler? ResultSearchChanged;

    public event SelectionChangedEventHandler? ResultSortChanged;

    public event TextChangedEventHandler? RootPathTextChanged;

    private void OnBrowse(object sender, RoutedEventArgs e) => BrowseRequested?.Invoke(sender, e);

    private void OnCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);

    private void OnCandidatesSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args) =>
        CandidatesSelectionChanged?.Invoke(sender, args);

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
