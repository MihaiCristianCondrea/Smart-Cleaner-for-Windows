using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.SettingsUi.Markup;

internal static class FluentExtensions
{
    public static T Assign<T>(this T target, Action<T> configure)
    {
        configure(target);
        return target;
    }

    public static StackPanel Spacing(this StackPanel panel, double spacing) =>
        panel.Assign(static (p, s) => p.Spacing = s, spacing);

    public static T Margin<T>(this T element, double uniformSize) where T : FrameworkElement =>
        element.Assign(static (e, size) => e.Margin = new Thickness(size), uniformSize);

    public static NavigationView OnLoaded(this NavigationView view, RoutedEventHandler handler) =>
        view.Assign((v, h) => v.Loaded += h, handler);

    public static NavigationView OnSelectionChanged(this NavigationView view, TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> handler) =>
        view.Assign((v, h) => v.SelectionChanged += h, handler);

    public static T WithContent<T>(this T control, object content) where T : ContentControl =>
        control.Assign((c, value) => c.Content = value, content);

    public static Panel Add(this Panel panel, UIElement child) =>
        panel.Assign((p, c) => p.Children.Add(c), child);

    private static T Assign<T, TValue>(this T target, Action<T, TValue> configure, TValue value)
    {
        configure(target, value);
        return target;
    }
}
