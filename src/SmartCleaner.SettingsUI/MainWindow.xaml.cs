using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartCleanerForWindows.Settings;
using SmartCleanerForWindows.SettingsUi.Markup;
using System.Text.Json.Nodes;

namespace SmartCleanerForWindows.SettingsUi;

public sealed class MainWindow : Window
{
    private readonly ToolSettingsService _settingsService;
    private readonly Dictionary<string, JsonObject> _pendingValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NavigationViewItem> _menuItems = new();
    private readonly NavigationView _toolsNavigation;
    private readonly StackPanel _fieldsHost;
    private ToolSettingsDefinition? _activeDefinition;

    public MainWindow()
    {
        Title = "Smart Cleaner Settings";

        _fieldsHost = new StackPanel().Spacing(12).Margin(24);

        _toolsNavigation = new NavigationView()
            .OnLoaded(OnNavigationLoaded)
            .OnSelectionChanged(OnNavigationSelectionChanged)
            .WithContent(new ScrollViewer().WithContent(_fieldsHost));

        Content = new Grid().Add(_toolsNavigation);

        _settingsService = ToolSettingsService.CreateDefault();
        _settingsService.SettingsChanged += OnSettingsChanged;
        Closed += OnWindowClosed;
    }

    private void OnNavigationLoaded(object sender, RoutedEventArgs e)
    {
        BuildMenu();
    }

    private void BuildMenu()
    {
        _menuItems.Clear();
        foreach (var definition in _settingsService.Definitions)
        {
            var item = new NavigationViewItem
            {
                Content = definition.Title,
                Tag = definition.Id,
                ToolTip = definition.Description,
                Icon = TryCreateIcon(definition.Icon)
            };
            _menuItems.Add(item);
        }

        _toolsNavigation.MenuItemsSource = null;
        _toolsNavigation.MenuItems.Clear();
        foreach (var item in _menuItems)
        {
            _toolsNavigation.MenuItems.Add(item);
        }

        if (_menuItems.Count > 0)
        {
            _toolsNavigation.SelectedItem = _menuItems[0];
        }
    }

    private static IconElement? TryCreateIcon(string? icon)
    {
        if (Enum.TryParse<Symbol>(icon, ignoreCase: true, out var symbol))
        {
            return new SymbolIcon(symbol);
        }

        return null;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string toolId)
        {
            return;
        }

        if (_settingsService.GetSnapshot(toolId) is { } snapshot)
        {
            _activeDefinition = snapshot.Definition;
            _pendingValues[toolId] = snapshot.Values;
            RenderFields(snapshot.Definition, snapshot.Values);
        }
    }

    private void RenderFields(ToolSettingsDefinition definition, JsonObject values)
    {
        _fieldsHost.Children.Clear();

        var header = new TextBlock()
            .Text(definition.Description ?? definition.Title)
            .Assign(tb =>
            {
                tb.Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style;
                tb.TextWrapping = TextWrapping.Wrap;
            });
        _fieldsHost.Children.Add(header);

        foreach (var field in definition.Fields)
        {
            UIElement element = field.FieldType switch
            {
                ToolSettingFieldType.Boolean => CreateToggle(field, values),
                ToolSettingFieldType.Number => CreateNumberBox(field, values),
                _ => CreateTextBox(field, values)
            };

            _fieldsHost.Children.Add(element);
        }
    }

    private UIElement CreateToggle(ToolSettingField field, JsonObject values)
    {
        var container = CreateFieldContainer(field);
        var toggle = new ToggleSwitch()
            .Header(field.DisplayName)
            .IsOn(values.TryGetPropertyValue(field.Key, out var node) && node?.GetValue<bool>() == true)
            .Assign(t => t.Tag = field);
        toggle.Toggled += (_, _) => PersistBoolean(toggle);
        container.Children.Add(toggle);
        return container;
    }

    private UIElement CreateNumberBox(ToolSettingField field, JsonObject values)
    {
        var container = CreateFieldContainer(field);
        var numberBox = new NumberBox()
            .Header(field.DisplayName)
            .Value(values.TryGetPropertyValue(field.Key, out var node) ? node?.GetValue<double>() ?? 0 : 0)
            .Assign(b =>
            {
                b.Minimum = field.Minimum ?? double.MinValue;
                b.Maximum = field.Maximum ?? double.MaxValue;
                b.SmallChange = field.Step ?? 1;
                b.Tag = field;
            });
        numberBox.ValueChanged += (_, args) => PersistNumber(numberBox, args.NewValue);
        container.Children.Add(numberBox);
        return container;
    }

    private UIElement CreateTextBox(ToolSettingField field, JsonObject values)
    {
        var container = CreateFieldContainer(field);
        var textBox = new TextBox()
            .Header(field.DisplayName)
            .Text(values.TryGetPropertyValue(field.Key, out var node) ? node?.ToString() ?? string.Empty : string.Empty)
            .Assign(tb => tb.Tag = field);
        textBox.TextChanged += (_, _) => PersistText(textBox);
        container.Children.Add(textBox);
        return container;
    }

    private static StackPanel CreateFieldContainer(ToolSettingField field)
    {
        var panel = new StackPanel().Spacing(4);
        if (!string.IsNullOrWhiteSpace(field.Description))
        {
            panel.Children.Add(new TextBlock()
                .Text(field.Description)
                .Margin(0, 0, 0, 4)
                .Assign(tb =>
                {
                    tb.Opacity = 0.7;
                    tb.TextWrapping = TextWrapping.Wrap;
                }));
        }

        return panel;
    }

    private void PersistBoolean(ToggleSwitch toggle)
    {
        if (toggle.Tag is not ToolSettingField field || _activeDefinition is null)
        {
            return;
        }

        UpdateValue(field.Key, JsonValue.Create(toggle.IsOn));
    }

    private void PersistNumber(NumberBox numberBox, double value)
    {
        if (numberBox.Tag is not ToolSettingField field || _activeDefinition is null)
        {
            return;
        }

        UpdateValue(field.Key, JsonValue.Create(value));
    }

    private void PersistText(TextBox textBox)
    {
        if (textBox.Tag is not ToolSettingField field || _activeDefinition is null)
        {
            return;
        }

        UpdateValue(field.Key, JsonValue.Create(textBox.Text));
    }

    private async void UpdateValue(string key, JsonNode? value)
    {
        if (_activeDefinition is null || value is null)
        {
            return;
        }

        if (!_pendingValues.TryGetValue(_activeDefinition.Id, out var values))
        {
            values = new JsonObject();
            _pendingValues[_activeDefinition.Id] = values;
        }

        values[key] = value;
        await _settingsService.UpdateAsync(_activeDefinition.Id, values);
    }

    private void OnSettingsChanged(object? sender, ToolSettingsChangedEventArgs e)
    {
        if (_activeDefinition is null || e.Snapshot.Definition.Id != _activeDefinition.Id)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() => RenderFields(e.Snapshot.Definition, e.Snapshot.Values));
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _settingsService.Dispose();
    }
}
