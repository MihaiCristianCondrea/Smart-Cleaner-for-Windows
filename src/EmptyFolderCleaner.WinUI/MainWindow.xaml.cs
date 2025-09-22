using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmptyFolderCleaner.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EmptyFolderCleaner.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _results = new();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        ResultsList.ItemsSource = _results;
    }

    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            RootPathBox.Text = folder.Path;
        }
    }

    private async void OnPreview(object sender, RoutedEventArgs e) => await RunScanAsync(delete: false);

    private async void OnDelete(object sender, RoutedEventArgs e) => await RunScanAsync(delete: true);

    private async Task RunScanAsync(bool delete)
    {
        CancelScan();
        var root = RootPathBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            ShowInfo("Select a valid root folder before scanning.", InfoBarSeverity.Warning);
            return;
        }

        ToggleBusyState(isBusy: true);
        _cts = new CancellationTokenSource();

        var options = new DirectoryCleanOptions
        {
            DryRun = !delete,
            SendToRecycleBin = RecycleCheckBox.IsChecked == true,
            SkipReparsePoints = IncludeReparseCheckBox.IsChecked != true,
            DeleteRootWhenEmpty = DeleteRootCheckBox.IsChecked == true,
            ExcludedNamePatterns = ParsePatternList()
        };

        try
        {
            var result = await Task.Run(() => DirectoryCleaner.Clean(root, options, _cts.Token));
            UpdateResults(result, options);
        }
        catch (OperationCanceledException)
        {
            ShowInfo("Operation cancelled.", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowInfo(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            ToggleBusyState(isBusy: false, options: options);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private IReadOnlyCollection<string> ParsePatternList()
    {
        var text = ExcludePatternsBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pattern => pattern.Trim())
            .Where(pattern => !string.IsNullOrEmpty(pattern))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void UpdateResults(DirectoryCleanResult result, DirectoryCleanOptions options)
    {
        _results.Clear();
        foreach (var path in result.EmptyDirectories)
        {
            _results.Add(path);
        }

        DeleteButton.IsEnabled = options.DryRun && _results.Count > 0;

        var message = options.DryRun
            ? $"Found {result.EmptyFound} empty directories."
            : $"Deleted {result.DeletedCount} of {result.EmptyFound} empty directories.";

        var severity = options.DryRun ? InfoBarSeverity.Informational : InfoBarSeverity.Success;
        if (result.HasFailures)
        {
            message += $" Encountered {result.Failures.Count} errors.";
            severity = InfoBarSeverity.Warning;
        }

        ShowInfo(message, severity);
    }

    private void ToggleBusyState(bool isBusy, DirectoryCleanOptions? options = null)
    {
        Progress.IsActive = isBusy;
        Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        PreviewButton.IsEnabled = !isBusy;
        DeleteButton.IsEnabled = !isBusy && options is not null && options.DryRun && _results.Count > 0;
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        StatusInfo.Message = message;
        StatusInfo.Severity = severity;
        StatusInfo.IsOpen = true;
    }

    private void CancelScan()
    {
        if (_cts is null)
        {
            return;
        }

        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }
}
