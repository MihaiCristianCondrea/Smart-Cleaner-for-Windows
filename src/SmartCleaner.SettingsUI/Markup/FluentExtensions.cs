using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.SettingsUi.Markup;

internal static class CSharpMarkupExtensions
{
    public static T Assign<T>(this T target, Action<T> configure)
    {
        configure(target);
        return target;
    }

    public static T Assign<T, TValue>(this T target, Action<T, TValue> configure, TValue value)
    {
        configure(target, value);
        return target;
    }

    public static T Margin<T>(this T element, double uniformSize) where T : FrameworkElement =>
        element.Assign(static (e, size) => e.Margin = new Thickness(size), uniformSize);

    public static T Margin<T>(this T element, double left, double top, double right, double bottom) where T : FrameworkElement =>
        element.Assign(static (e, m) => e.Margin = m, new Thickness(left, top, right, bottom));

    public static StackPanel Spacing(this StackPanel panel, double spacing) =>
        panel.Assign(static (p, s) => p.Spacing = s, spacing);

    public static TextBlock Text(this TextBlock textBlock, string value) =>
        textBlock.Assign(static (tb, v) => tb.Text = v, value);

    public static TextBlock Wrap(this TextBlock textBlock, TextWrapping wrapping = TextWrapping.Wrap) =>
        textBlock.Assign(static (tb, value) => tb.TextWrapping = value, wrapping);

    public static TextBox Header(this TextBox textBox, string value) =>
        textBox.Assign(static (tb, v) => tb.Header = v, value);

    public static NumberBox Header(this NumberBox box, string value) =>
        box.Assign(static (b, v) => b.Header = v, value);

    public static ToggleSwitch Header(this ToggleSwitch toggle, string value) =>
        toggle.Assign(static (t, v) => t.Header = v, value);

    public static TextBox Text(this TextBox textBox, string value) =>
        textBox.Assign(static (tb, v) => tb.Text = v, value);

    public static ToggleSwitch IsOn(this ToggleSwitch toggle, bool value) =>
        toggle.Assign(static (t, v) => t.IsOn = v, value);

    public static NumberBox Value(this NumberBox box, double value) =>
        box.Assign(static (b, v) => b.Value = v, value);

    public static NumberBox Range(this NumberBox box, double minimum, double maximum) =>
        box.Assign(static (b, values) =>
        {
            b.Minimum = values.minimum;
            b.Maximum = values.maximum;
        }, (minimum, maximum));

    public static NumberBox Step(this NumberBox box, double step) =>
        box.Assign(static (b, value) => b.SmallChange = value, step);

    public static NavigationView OnLoaded(this NavigationView view, RoutedEventHandler handler) =>
        view.Assign(static (v, h) => v.Loaded += h, handler);

    public static NavigationView OnSelectionChanged(this NavigationView view, TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> handler) =>
        view.Assign(static (v, h) => v.SelectionChanged += h, handler);

    public static NavigationViewItem Title(this NavigationViewItem item, string value) =>
        item.Assign(static (i, v) => i.Content = v, value);

    public static NavigationViewItem Description(this NavigationViewItem item, string? value) =>
        item.Assign(static (i, v) => i.ToolTip = v, value);

    public static NavigationViewItem Key(this NavigationViewItem item, string value) =>
        item.Assign(static (i, v) => i.Tag = v, value);

    public static NavigationViewItem Icon(this NavigationViewItem item, IconElement? value) =>
        item.Assign(static (i, v) => i.Icon = v, value);

    public static T WithContent<T>(this T control, object content) where T : ContentControl =>
        control.Assign(static (c, value) => c.Content = value, content);

    public static Panel Add(this Panel panel, UIElement child) =>
        panel.Assign(static (p, c) => p.Children.Add(c), child);
}
