using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using SmartCleanerForWindows.Common.Markup;
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
        var rootStack = new StackPanel().Spacing(24).Assign(panel => panel.Padding = new Thickness(32));
        var storageCard = BuildStorageCard();

        rootStack
            .Add(BuildHeader())
            .Add(storageCard.Card)
            .Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.Play }.Foreground(GetBrush("TextOnAccentFillColorPrimaryBrush")), "Empty folders cleaner", "Scan folders and preview empty directories before deleting.", "Open empty folders cleaner", OnNavigateToEmptyFolders, Symbol.Play))
            .Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.SaveLocal }.Foreground(GetBrush("TextOnAccentFillColorPrimaryBrush")), "Large files explorer", "Find the biggest files by type and reclaim space quickly.", "Open large files explorer", OnNavigateToLargeFiles, Symbol.SaveLocal))
            .Add(BuildToolCard(new FontIcon { Glyph = "\uE74D", FontFamily = new FontFamily("Segoe MDL2 Assets") }.Foreground(GetBrush("TextOnAccentFillColorPrimaryBrush")), "Disk cleanup", "Analyze Windows cleanup handlers and free up disk space.", "Open disk cleanup", OnNavigateToDiskCleanup, Symbol.Delete))
            .Add(BuildToolCard(new SymbolIcon { Symbol = Symbol.Globe }.Foreground(GetBrush("TextOnAccentFillColorPrimaryBrush")), "Internet repair", "Run guided fixes for common connectivity problems.", "Open internet repair", OnNavigateToInternetRepair, Symbol.Globe));

        return (new ScrollViewer().Content(rootStack).Assign(v => v.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled), storageCard.StorageSummaryText, storageCard.StorageTipText, storageCard.DriveUsageList);
    }

    private UIElement BuildHeader() =>
        new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        }.Spacing(16)
         .Add(new Border()
            .Width(56)
            .Height(56)
            .CornerRadius(new CornerRadius(18))
            .Background(GetBrush("AccentFillColorTertiaryBrush"))
            .Assign(b => b.VerticalAlignment = VerticalAlignment.Center)
            .Child(new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png")),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }.Width(36).Height(36)))
         .Add(new StackPanel { VerticalAlignment = VerticalAlignment.Center }.Spacing(4)
            .Add(new TextBlock().Text("Dashboard").FontSize(32).Assign(t => t.FontWeight = FontWeights.SemiBold))
            .Add(new TextBlock().Text("Choose a tool to continue.").Foreground(GetBrush("SystemControlForegroundBaseMediumBrush"))));

    private (Border Card, TextBlock StorageSummaryText, TextBlock StorageTipText, ItemsControl DriveUsageList) BuildStorageCard()
    {
        var summaryText = new TextBlock()
            .Text("Collecting drive information…")
            .Foreground(GetBrush("SystemControlForegroundBaseMediumBrush"))
            .Wrap(TextWrapping.WrapWholeWords);

        var tipText = new TextBlock()
            .Text("Keep an eye on your drives to maintain performance.")
            .Foreground(GetBrush("SystemControlForegroundBaseMediumBrush"))
            .Wrap(TextWrapping.WrapWholeWords);

        var itemsControl = new ItemsControl
        {
            ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Spacing='12' /></ItemsPanelTemplate>"),
            ItemTemplate = CreateDriveUsageTemplate(),
        };

        var cardContent = new StackPanel().Spacing(16)
            .Add(new StackPanel { Orientation = Orientation.Horizontal }.Spacing(12)
                .Add(BuildIconContainer(new FontIcon { Glyph = "\uE8A0", FontFamily = new FontFamily("Segoe MDL2 Assets") }.Foreground(GetBrush("TextOnAccentFillColorPrimaryBrush"))))
                .Add(new StackPanel()
                    .Add(new TextBlock().Text("Storage overview").FontSize(22).Assign(t => t.FontWeight = FontWeights.SemiBold))
                    .Add(summaryText)))
            .Add(itemsControl)
            .Add(new StackPanel { Orientation = Orientation.Horizontal }.Spacing(8)
                .Add(new FontIcon
                {
                    Glyph = "\uE946",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    VerticalAlignment = VerticalAlignment.Center,
                }.Foreground(GetBrush("SystemControlForegroundBaseMediumBrush")))
                .Add(tipText));

        return (BuildCard(cardContent), summaryText, tipText, itemsControl);
    }

    private Border BuildToolCard(IconElement leadingIcon, string title, string description, string buttonText, RoutedEventHandler handler, Symbol symbol)
    {
        var content = new StackPanel().Spacing(16)
            .Add(new StackPanel { Orientation = Orientation.Horizontal }.Spacing(12)
                .Add(BuildIconContainer(leadingIcon))
                .Add(new StackPanel()
                    .Add(new TextBlock().Text(title).FontSize(22).Assign(t => t.FontWeight = FontWeights.SemiBold))
                    .Add(new TextBlock().Text(description).Foreground(GetBrush("SystemControlForegroundBaseMediumBrush")).Wrap())))
            .Add(new Button()
                .Assign(button => button.Click += handler)
                .Content(new StackPanel { Orientation = Orientation.Horizontal }
                    .Spacing(8)
                    .Add(new SymbolIcon { Symbol = symbol })
                    .Add(new TextBlock().Text(buttonText))));

        return BuildCard(content);
    }

    private static Border BuildIconContainer(IconElement icon) =>
        new Border()
            .Width(48)
            .Height(48)
            .CornerRadius(new CornerRadius(16))
            .Background(GetBrush("AccentFillColorTertiaryBrush"))
            .Assign(b => b.VerticalAlignment = VerticalAlignment.Center)
            .Child(new Viewbox { Child = icon });

    private static Border BuildCard(UIElement content) =>
        new Border()
            .Background(GetBrush("LayerFillColorDefaultBrush"))
            .BorderBrush(GetBrush("CardStrokeColorDefaultBrush"))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(20))
            .Assign(b => b.Padding = new Thickness(24))
            .Child(content);

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

    private static Brush GetBrush(string key) => (Brush)Application.Current.Resources[key];

    private void OnNavigateToEmptyFolders(object sender, RoutedEventArgs e) =>
        NavigateToEmptyFoldersRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToLargeFiles(object sender, RoutedEventArgs e) =>
        NavigateToLargeFilesRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToDiskCleanup(object sender, RoutedEventArgs e) =>
        NavigateToDiskCleanupRequested?.Invoke(this, EventArgs.Empty);

    private void OnNavigateToInternetRepair(object sender, RoutedEventArgs e) =>
        NavigateToInternetRepairRequested?.Invoke(this, EventArgs.Empty);
}
