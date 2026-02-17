using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using SmartCleanerForWindows.Diagnostics;

namespace SmartCleanerForWindows.Modules.DiskCleanup.Views;

public sealed class DiskCleanupView : UserControl
{
    internal TextBlock DiskCleanupIntro { get; }
    internal TextBlock DiskCleanupStatusText { get; }
    internal TextBlock DiskCleanupActivityText { get; }
    internal Button DiskCleanupAnalyzeBtn { get; }
    internal Button DiskCleanupCleanBtn { get; }
    internal Button DiskCleanupCancelBtn { get; }
    internal ProgressBar DiskCleanupProgress { get; }
    internal InfoBar DiskCleanupInfoBar { get; }
    internal ListView DiskCleanupList { get; }

    public DiskCleanupView()
    {
        DiskCleanupIntro = new TextBlock
        {
            Text = "Analyze Windows cleanup handlers to reclaim temporary files.",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)),
            TextWrapping = TextWrapping.Wrap
        };

        DiskCleanupStatusText = new TextBlock
        {
            Text = "Ready to analyze Windows disk cleanup handlers.",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)),
            TextWrapping = TextWrapping.Wrap
        };

        DiskCleanupActivityText = new TextBlock
        {
            Text = "Waiting for the next action.",
            TextWrapping = TextWrapping.Wrap
        };
        DiskCleanupActivityText.Foreground = TryGetBrush("SystemControlForegroundBaseMediumBrush");

        DiskCleanupAnalyzeBtn = CreateActionButton(Symbol.Refresh, "Analyze", OnDiskCleanupAnalyze);
        DiskCleanupCleanBtn = CreateActionButton(Symbol.Delete, "Clean selected", OnDiskCleanupClean);
        DiskCleanupCleanBtn.IsEnabled = false;
        DiskCleanupCancelBtn = CreateActionButton(Symbol.Cancel, "Cancel", OnCancel);
        DiskCleanupCancelBtn.Visibility = Visibility.Collapsed;
        DiskCleanupCancelBtn.IsEnabled = false;

        DiskCleanupProgress = new ProgressBar
        {
            Height = 4,
            Visibility = Visibility.Collapsed,
            IsIndeterminate = true
        };

        DiskCleanupInfoBar = new InfoBar { IsOpen = false };

        DiskCleanupList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = false,
            ItemTemplate = CreateItemTemplate()
        };

        Content = CreateLayout();
        UiConstructionLog.AttachFrameworkElementDiagnostics(this, "DiskCleanupView");
    }

    public event RoutedEventHandler? AnalyzeRequested;
    public event RoutedEventHandler? CleanRequested;
    public event RoutedEventHandler? CancelRequested;

    private UIElement CreateLayout()
    {
        var heroStack = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        heroStack.Children.Add(new TextBlock
        {
            Text = "Disk cleanup",
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)),
            FontWeight = Windows.UI.Text.FontWeights.SemiBold
        });
        heroStack.Children.Add(DiskCleanupIntro);
        heroStack.Children.Add(DiskCleanupStatusText);

        var heroGrid = new Grid { ColumnSpacing = 24 };
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        heroGrid.Children.Add(CreateIconBadge(76, 24, "\uE74D", "HeroCardOverlayBrush", "White"));
        Grid.SetColumn(heroStack, 1);
        heroGrid.Children.Add(heroStack);

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        headerRow.Children.Add(CreateIconBadge(40, 14, "\uE74D", "AccentFillColorTertiaryBrush", "TextOnAccentFillColorPrimaryBrush", true));
        headerRow.Children.Add(new TextBlock { Text = "Cleanup categories", FontSize = 22, FontWeight = Windows.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        actions.Children.Add(DiskCleanupAnalyzeBtn);
        actions.Children.Add(DiskCleanupCleanBtn);
        actions.Children.Add(DiskCleanupCancelBtn);

        var contentStack = new StackPanel { Spacing = 16 };
        contentStack.Children.Add(headerRow);
        contentStack.Children.Add(DiskCleanupActivityText);
        contentStack.Children.Add(actions);
        contentStack.Children.Add(DiskCleanupProgress);
        contentStack.Children.Add(DiskCleanupInfoBar);
        contentStack.Children.Add(DiskCleanupList);

        var contentBorder = new Border
        {
            Padding = new Thickness(24),
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1),
            Child = contentStack
        };
        contentBorder.Background = TryGetBrush("LayerFillColorDefaultBrush");
        contentBorder.BorderBrush = TryGetBrush("CardStrokeColorDefaultBrush");

        var heroBorder = new Border
        {
            Padding = new Thickness(28),
            CornerRadius = new CornerRadius(28),
            Child = heroGrid
        };
        heroBorder.Background = TryGetBrush("AccentFillColorTertiaryBrush");

        var rootGrid = new Grid { Padding = new Thickness(32), RowSpacing = 24 };
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        rootGrid.Children.Add(heroBorder);
        Grid.SetRow(contentBorder, 1);
        rootGrid.Children.Add(contentBorder);

        return new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = rootGrid };
    }

    private static Border CreateIconBadge(double size, double cornerRadius, string glyph, string backgroundResource, string foregroundResource, bool useThemeForeground = false)
    {
        var icon = new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (useThemeForeground)
        {
            icon.Foreground = TryGetBrush(foregroundResource);
        }
        else
        {
            icon.Foreground = foregroundResource == "White"
                ? new SolidColorBrush(Windows.UI.Colors.White)
                : new SolidColorBrush(Windows.UI.Colors.Black);
        }

        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(cornerRadius),
            VerticalAlignment = VerticalAlignment.Center,
            Child = icon
        };
        border.Background = TryGetBrush(backgroundResource);
        return border;
    }


    private static Brush TryGetBrush(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Windows.UI.Colors.Transparent);
    }

    private static Button CreateActionButton(Symbol symbol, string label, RoutedEventHandler onClick)
    {
        var button = new Button();
        button.Click += onClick;
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(new SymbolIcon(symbol));
        content.Children.Add(new TextBlock { Text = label });
        button.Content = content;
        return button;
    }

    private static DataTemplate CreateItemTemplate()
    {
        const string templateXaml = """
<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
    <CheckBox Padding='0' HorizontalContentAlignment='Stretch' IsChecked='{Binding IsSelected, Mode=TwoWay}' IsEnabled='{Binding CanSelect}'>
        <Grid ColumnSpacing='12'>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width='Auto' />
                <ColumnDefinition Width='*' />
                <ColumnDefinition Width='Auto' />
            </Grid.ColumnDefinitions>
            <Border Width='32' Height='32' CornerRadius='12' Background='{ThemeResource AccentFillColorTertiaryBrush}' HorizontalAlignment='Center' VerticalAlignment='Center'>
                <FontIcon Glyph='&#xE74D;' FontFamily='Segoe MDL2 Assets' Foreground='{ThemeResource TextOnAccentFillColorPrimaryBrush}' HorizontalAlignment='Center' VerticalAlignment='Center' />
            </Border>
            <StackPanel Grid.Column='1' Spacing='2'>
                <TextBlock Text='{Binding Name}' FontWeight='SemiBold' TextWrapping='Wrap' />
                <TextBlock Text='{Binding Description}' Foreground='{ThemeResource SystemControlForegroundBaseMediumBrush}' TextWrapping='Wrap' />
                <TextBlock Text='{Binding ErrorMessage}' Foreground='{ThemeResource SystemFillColorCriticalBrush}' TextWrapping='Wrap' />
            </StackPanel>
            <TextBlock Grid.Column='2' Text='{Binding FormattedSize}' HorizontalAlignment='Right' VerticalAlignment='Center' FontWeight='SemiBold' />
        </Grid>
    </CheckBox>
</DataTemplate>
""";

        return (DataTemplate)XamlReader.Load(templateXaml);
    }

    private void OnDiskCleanupAnalyze(object sender, RoutedEventArgs e) => AnalyzeRequested?.Invoke(sender, e);
    private void OnDiskCleanupClean(object sender, RoutedEventArgs e) => CleanRequested?.Invoke(sender, e);
    private void OnCancel(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(sender, e);
}
