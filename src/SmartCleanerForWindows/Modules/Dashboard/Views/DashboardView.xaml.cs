using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using SmartCleanerForWindows.Diagnostics;

namespace SmartCleanerForWindows.Modules.Dashboard.Views;

public sealed partial class DashboardView : UserControl
{
    private readonly TextBlock storageSummaryText;
    private readonly TextBlock storageTipText;
    private readonly ItemsControl driveUsageList;

    public DashboardView()
    {
        var layout = BuildLayout();
        Content = layout.Layout;

        storageSummaryText = layout.StorageSummaryText;
        storageTipText = layout.StorageTipText;
        driveUsageList = layout.DriveUsageList;

        UiConstructionLog.AttachFrameworkElementDiagnostics(this, "DashboardView");
    }

    public TextBlock StorageSummaryTextBlock => storageSummaryText;

    public TextBlock StorageTipTextBlock => storageTipText;

    public ItemsControl DriveUsageListControl => driveUsageList;

    public event EventHandler? NavigateToEmptyFoldersRequested;

    public event EventHandler? NavigateToLargeFilesRequested;

    public event EventHandler? NavigateToDiskCleanupRequested;

    public event EventHandler? NavigateToInternetRepairRequested;

    private (UIElement Layout, TextBlock StorageSummaryText, TextBlock StorageTipText, ItemsControl DriveUsageList) BuildLayout()
    {
        var rootStack = new StackPanel { Padding = new Thickness(32), Spacing = 24 };

        rootStack.Children.Add(BuildHeader());

        var storageCard = BuildStorageCard();
        rootStack.Children.Add(storageCard.Card);

        rootStack.Children.Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.Play, Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] }, "Empty folders cleaner", "Scan folders and preview empty directories before deleting.", "Open empty folders cleaner", OnNavigateToEmptyFolders, Symbol.Play));
        rootStack.Children.Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.SaveLocal, Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] }, "Large files explorer", "Find the biggest files by type and reclaim space quickly.", "Open large files explorer", OnNavigateToLargeFiles, Symbol.SaveLocal));
        rootStack.Children.Add(BuildToolCard(new FontIcon { Glyph = "\uE74D", FontFamily = new FontFamily("Segoe MDL2 Assets"), Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] }, "Disk cleanup", "Analyze Windows cleanup handlers and free up disk space.", "Open disk cleanup", OnNavigateToDiskCleanup, Symbol.Delete));
        rootStack.Children.Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.Globe, Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] }, "Internet repair", "Run guided fixes for common connectivity problems.", "Open internet repair", OnNavigateToInternetRepair, Symbol.Globe));

        return (new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = rootStack,
        }, storageCard.StorageSummaryText, storageCard.StorageTipText, storageCard.DriveUsageList);
    }

    private UIElement BuildHeader() =>
        new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Border
                {
                    Width = 56,
                    Height = 56,
                    CornerRadius = new CornerRadius(18),
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"],
                    Child = new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png")),
                        Stretch = Stretch.Uniform,
                        Width = 36,
                        Height = 36,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                },
                new StackPanel
                {
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = "Dashboard", FontSize = 32, FontWeight = FontWeights.SemiBold },
                        new TextBlock
                        {
                            Text = "Choose a tool to continue.",
                            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
                        },
                    },
                },
            },
        };

    private (Border Card, TextBlock StorageSummaryText, TextBlock StorageTipText, ItemsControl DriveUsageList) BuildStorageCard()
    {
        var summaryText = new TextBlock
        {
            Text = "Collecting drive information…",
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        var tipText = new TextBlock
        {
            Text = "Keep an eye on your drives to maintain performance.",
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
            TextWrapping = TextWrapping.WrapWholeWords,
        };

        var itemsControl = new ItemsControl();
        itemsControl.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Spacing='12' /></ItemsPanelTemplate>");
        itemsControl.ItemTemplate = CreateDriveUsageTemplate();

        var cardContent = new StackPanel { Spacing = 16 };
        cardContent.Children.Add(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    BuildIconContainer(new FontIcon { Glyph = "\uE8A0", FontFamily = new FontFamily("Segoe MDL2 Assets"), Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] }),
                    new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Storage overview", FontSize = 22, FontWeight = FontWeights.SemiBold },
                            summaryText,
                        },
                    },
                },
            });

        cardContent.Children.Add(itemsControl);

        cardContent.Children.Add(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = "\uE946",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    tipText,
                },
            });

        return (BuildCard(cardContent), summaryText, tipText, itemsControl);
    }

    private Border BuildToolCard(IconElement leadingIcon, string title, string description, string buttonText, RoutedEventHandler handler, Symbol symbol)
    {
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    BuildIconContainer(leadingIcon),
                    new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeights.SemiBold },
                            new TextBlock
                            {
                                Text = description,
                                Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
                                TextWrapping = TextWrapping.Wrap,
                            },
                        },
                    },
                },
            });

        var button = new Button();
        button.Click += handler;
        button.Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { new SymbolIcon { Symbol = symbol }, new TextBlock { Text = buttonText } },
        };
        content.Children.Add(button);

        return BuildCard(content);
    }

    private static Border BuildIconContainer(IconElement icon) =>
        new()
        {
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(16),
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"],
            Child = new Viewbox { Child = icon },
        };

    private static Border BuildCard(UIElement content) =>
        new()
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(24),
            Child = content,
        };

    private static DataTemplate CreateDriveUsageTemplate() =>
        (DataTemplate)XamlReader.Load(
            """
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Grid ColumnSpacing='12'>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width='*' />
                        <ColumnDefinition Width='160' />
                    </Grid.ColumnDefinitions>
                    <StackPanel Spacing='4'>
                        <TextBlock Text='{Binding Name}' FontWeight='SemiBold' />
                        <TextBlock Text='{Binding Details}' Foreground='{ThemeResource SystemControlForegroundBaseMediumBrush}' TextWrapping='Wrap' />
                    </StackPanel>
                    <StackPanel Grid.Column='1' Spacing='4' VerticalAlignment='Center'>
                        <ProgressBar Minimum='0' Maximum='100' Value='{Binding UsedPercentage}' Height='6' />
                        <TextBlock Text='{Binding UsageSummary}' HorizontalAlignment='Right' Foreground='{ThemeResource SystemControlForegroundBaseMediumBrush}' />
                    </StackPanel>
                </Grid>
            </DataTemplate>
            """);

    private void OnNavigateToEmptyFolders(object sender, RoutedEventArgs e) =>
        NavigateToEmptyFoldersRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToLargeFiles(object sender, RoutedEventArgs e) =>
        NavigateToLargeFilesRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToDiskCleanup(object sender, RoutedEventArgs e) =>
        NavigateToDiskCleanupRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToInternetRepair(object sender, RoutedEventArgs e) =>
        NavigateToInternetRepairRequested?.Invoke(this, EventArgs.Empty);
}
