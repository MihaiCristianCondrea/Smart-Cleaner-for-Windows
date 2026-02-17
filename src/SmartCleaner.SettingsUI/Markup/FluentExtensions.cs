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
        textBlock.Assign((tb, v) => tb.Text = v, value);

    public static TextBox Header(this TextBox textBox, string value) =>
        textBox.Assign((tb, v) => tb.Header = v, value);

    public static NumberBox Header(this NumberBox box, string value) =>
        box.Assign((b, v) => b.Header = v, value);

    public static ToggleSwitch Header(this ToggleSwitch toggle, string value) =>
        toggle.Assign((t, v) => t.Header = v, value);

    public static TextBox Text(this TextBox textBox, string value) =>
        textBox.Assign((tb, v) => tb.Text = v, value);

    public static ToggleSwitch IsOn(this ToggleSwitch toggle, bool value) =>
        toggle.Assign((t, v) => t.IsOn = v, value);

    public static NumberBox Value(this NumberBox box, double value) =>
        box.Assign((b, v) => b.Value = v, value);

    public static NavigationView OnLoaded(this NavigationView view, RoutedEventHandler handler) =>
        view.Assign((v, h) => v.Loaded += h, handler);

    public static NavigationView OnSelectionChanged(this NavigationView view, TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> handler) =>
        view.Assign((v, h) => v.SelectionChanged += h, handler);

    public static T WithContent<T>(this T control, object content) where T : ContentControl =>
        control.Assign((c, value) => c.Content = value, content);

    public static Panel Add(this Panel panel, UIElement child) =>
        panel.Assign((p, c) => p.Children.Add(c), child);
}
