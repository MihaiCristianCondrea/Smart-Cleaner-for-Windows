using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmptyFolderCleaner.Core;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;

namespace EmptyFolderCleaner.WinUI;

public sealed class MainWindow : Window
{
    private CancellationTokenSource? _cts;
    private MicaController? _mica;
    private SystemBackdropConfiguration? _backdropConfig;
    private List<string> _previewCandidates = new();
    private bool _isBusy;
    private readonly TextBox RootPathBox;
    private readonly Button BrowseBtn;
    private readonly Button PreviewBtn;
    private readonly Button DeleteBtn;
    private readonly CheckBox RecycleChk;
    private readonly NumberBox DepthBox;
    private readonly TextBox ExcludeBox;
    private readonly ProgressBar Progress;
    private readonly Button CancelBtn;
    private readonly InfoBar Info;
    private readonly ListView Candidates;

    public MainWindow()
    {
        Title = "Empty Folder Cleaner";
        Width = 840;
        Height = 600;

        var navigation = new NavigationView
        {
            IsSettingsVisible = false,
            PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal
        };

        var grid = new Grid { Padding = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        RootPathBox = new TextBox
        {
            Width = 520,
            PlaceholderText = "Select a root folder…"
        };
        RootPathBox.TextChanged += RootPathBox_TextChanged;

        BrowseBtn = new Button { Content = "Browse" };
        BrowseBtn.Click += OnBrowse;

        PreviewBtn = new Button { Content = "Preview" };
        PreviewBtn.Click += OnPreview;

        DeleteBtn = new Button
        {
            Content = "Delete",
            IsEnabled = false
        };
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyleObj) &&
            accentStyleObj is Style accentStyle)
        {
            DeleteBtn.Style = accentStyle;
        }
        DeleteBtn.Click += OnDelete;

        var pathRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        pathRow.Children.Add(RootPathBox);
        pathRow.Children.Add(BrowseBtn);
        pathRow.Children.Add(PreviewBtn);
        pathRow.Children.Add(DeleteBtn);

        RecycleChk = new CheckBox
        {
            Content = "Send to Recycle Bin",
            IsChecked = true
        };

        DepthBox = new NumberBox
        {
            Minimum = 0,
            Maximum = 999,
            Value = 0,
            Width = 90
        };

        var depthRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        depthRow.Children.Add(new TextBlock
        {
            Text = "Depth limit:",
            VerticalAlignment = VerticalAlignment.Center
        });
        depthRow.Children.Add(DepthBox);

        ExcludeBox = new TextBox
        {
            Width = 360,
            PlaceholderText = ".git; build/*; node_modules"
        };

        var excludeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        excludeRow.Children.Add(new TextBlock
        {
            Text = "Exclusions (semicolon-separated):",
            VerticalAlignment = VerticalAlignment.Center
        });
        excludeRow.Children.Add(ExcludeBox);

        var optionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        optionsRow.Children.Add(RecycleChk);
        optionsRow.Children.Add(depthRow);
        optionsRow.Children.Add(excludeRow);
        Grid.SetRow(optionsRow, 1);

        Progress = new ProgressBar
        {
            Width = 280,
            Visibility = Visibility.Collapsed,
            IsIndeterminate = true
        };

        CancelBtn = new Button
        {
            Content = "Cancel",
            Visibility = Visibility.Collapsed,
            IsEnabled = false
        };
        CancelBtn.Click += OnCancel;

        Info = new InfoBar { IsOpen = false };

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        statusRow.Children.Add(Progress);
        statusRow.Children.Add(CancelBtn);
        statusRow.Children.Add(Info);
        Grid.SetRow(statusRow, 2);

        Candidates = new ListView
        {
            Margin = new Thickness(0, 8, 0, 0),
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = false
        };
        Candidates.ItemTemplate = new DataTemplate(() =>
        {
            var textBlock = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis };
            textBlock.SetBinding(TextBlock.TextProperty, new Binding());
            return textBlock;
        });
        Grid.SetRow(Candidates, 3);

        grid.Children.Add(pathRow);
        grid.Children.Add(optionsRow);
        grid.Children.Add(statusRow);
        grid.Children.Add(Candidates);

        navigation.Content = grid;
        Content = navigation;

        TryEnableMica();
        TryApplyIcon();
        Activated += OnWindowActivated;
        Closed += OnClosed;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Activated -= OnWindowActivated;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _mica?.Dispose();
        _mica = null;
        _backdropConfig = null;
    }

    private void TryEnableMica()
    {
        if (!MicaController.IsSupported())
        {
            return;
        }

        _backdropConfig = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = Application.Current.RequestedTheme switch
            {
                ApplicationTheme.Dark => SystemBackdropTheme.Dark,
                ApplicationTheme.Light => SystemBackdropTheme.Light,
                _ => SystemBackdropTheme.Default
            }
        };

        _mica = new MicaController { Kind = MicaKind.Base };
        _mica.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
        _mica.SetSystemBackdropConfiguration(_backdropConfig);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_backdropConfig is not null)
        {
            _backdropConfig.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void TryApplyIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (!File.Exists(iconPath))
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(iconPath);
        }
        catch
        {
            // Ignore icon failures on unsupported systems.
        }
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
            DeleteBtn.IsEnabled = !_isBusy && _previewCandidates.Count > 0;
        }
    }

    private void RootPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        _previewCandidates.Clear();
        Candidates.ItemsSource = null;
        DeleteBtn.IsEnabled = false;
    }

    private async void OnPreview(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        if (!TryGetRootPath(out var root))
        {
            ShowInfo("Select a valid folder.", InfoBarSeverity.Warning);
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        _previewCandidates = new List<string>();
        Candidates.ItemsSource = null;
        DeleteBtn.IsEnabled = false;

        SetBusy(true);

        try
        {
            var options = CreateOptions(dryRun: true);
            var result = await Task.Run(() => DirectoryCleaner.Clean(root, options, _cts.Token));

            _previewCandidates = new List<string>(result.EmptyDirectories);
            Candidates.ItemsSource = _previewCandidates;
            DeleteBtn.IsEnabled = !_isBusy && _previewCandidates.Count > 0;

            var message = $"Found {result.EmptyFound} empty folder(s).";
            var severity = InfoBarSeverity.Informational;
            if (result.HasFailures)
            {
                message += $" Encountered {result.Failures.Count} issue(s).";
                severity = InfoBarSeverity.Warning;
            }

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            ShowInfo("Preview cancelled.", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        if (!TryGetRootPath(out var root))
        {
            ShowInfo("Select a valid folder.", InfoBarSeverity.Warning);
            return;
        }

        CancelActiveOperation();
        _cts = new CancellationTokenSource();
        SetBusy(true);

        try
        {
            var options = CreateOptions(dryRun: false);
            var result = await Task.Run(() => DirectoryCleaner.Clean(root, options, _cts.Token));

            _previewCandidates.Clear();
            Candidates.ItemsSource = null;
            DeleteBtn.IsEnabled = false;

            var message = result.EmptyFound == 0
                ? "No empty folders detected."
                : $"Deleted {result.DeletedCount} folder(s).";
            var severity = result.EmptyFound == 0 ? InfoBarSeverity.Informational : InfoBarSeverity.Success;

            if (result.EmptyFound > result.DeletedCount)
            {
                var remaining = result.EmptyFound - result.DeletedCount;
                message += $" {remaining} item(s) could not be removed.";
            }

            if (result.HasFailures)
            {
                message += $" Encountered {result.Failures.Count} issue(s).";
                severity = InfoBarSeverity.Warning;
            }

            ShowInfo(message, severity);
        }
        catch (OperationCanceledException)
        {
            ShowInfo("Deletion cancelled.", InfoBarSeverity.Informational);
        }
        catch (UnauthorizedAccessException)
        {
            ShowInfo("Access denied. Try running as Administrator.", InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            ShowInfo($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => CancelActiveOperation();

    private void CancelActiveOperation()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
        }
    }

    private bool TryGetRootPath(out string root)
    {
        root = RootPathBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    private DirectoryCleanOptions CreateOptions(bool dryRun)
    {
        var depthValue = DepthBox.Value;
        int? maxDepth = null;
        if (!double.IsNaN(depthValue))
        {
            var depth = (int)Math.Max(0, Math.Round(depthValue));
            if (depth > 0)
            {
                maxDepth = depth;
            }
        }

        return new DirectoryCleanOptions
        {
            DryRun = dryRun,
            SendToRecycleBin = RecycleChk.IsChecked == true,
            SkipReparsePoints = true,
            DeleteRootWhenEmpty = false,
            MaxDepth = maxDepth,
            ExcludedNamePatterns = ParseExclusions(ExcludeBox.Text),
        };
    }

    private static IReadOnlyCollection<string> ParseExclusions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        Progress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        Progress.IsIndeterminate = isBusy;
        CancelBtn.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        CancelBtn.IsEnabled = isBusy;
        PreviewBtn.IsEnabled = !isBusy;
        DeleteBtn.IsEnabled = !isBusy && _previewCandidates.Count > 0;
        BrowseBtn.IsEnabled = !isBusy;
        RootPathBox.IsEnabled = !isBusy;
        DepthBox.IsEnabled = !isBusy;
        ExcludeBox.IsEnabled = !isBusy;
        RecycleChk.IsEnabled = !isBusy;
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        Info.Message = message;
        Info.Severity = severity;
        Info.IsOpen = true;
    }
}
