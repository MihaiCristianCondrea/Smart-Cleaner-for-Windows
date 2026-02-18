using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SmartCleanerForWindows.Common.Markup;

internal static class CSharpMarkupExtensions
{
    public static T Assign<T>(this T target, Action<T> configure)
    {
        configure(target);
        return target;
    }

    public static T Assign<T, TValue>(this T target, Action<T, TValue> configure, TValue value)
    {
        configure(target);
        return target;
    }

    public static Panel Add(this Panel panel, UIElement child) =>
        panel.Assign(static (p, c) => p.Children.Add(c), child);

    public static T GridRow<T>(this T element, int row) where T : UIElement =>
        element.Assign(static (e, r) => Grid.SetRow(e, r), row);

    public static T GridColumn<T>(this T element, int column) where T : UIElement =>
        element.Assign(static (e, c) => Grid.SetColumn(e, c), column);

    public static T GridColumnSpan<T>(this T element, int span) where T : UIElement =>
        element.Assign(static (e, s) => Grid.SetColumnSpan(e, s), span);

    public static T Spacing<T>(this T panel, double spacing) where T : StackPanel =>
        panel.Assign(static (p, s) => p.Spacing = s, spacing);

    public static T Padding<T>(this T element, Thickness thickness) where T : Control =>
        element.Assign(static (e, t) => e.Padding = t, thickness);

    public static T Background<T>(this T border, Brush brush) where T : Border =>
        border.Assign(static (b, value) => b.Background = value, brush);

    public static T CornerRadius<T>(this T border, CornerRadius radius) where T : Border =>
        border.Assign(static (b, value) => b.CornerRadius = value, radius);

    public static T BorderBrush<T>(this T border, Brush brush) where T : Border =>
        border.Assign(static (b, value) => b.BorderBrush = value, brush);

    public static T BorderThickness<T>(this T border, Thickness thickness) where T : Border =>
        border.Assign(static (b, value) => b.BorderThickness = value, thickness);

    public static T Child<T>(this T border, UIElement child) where T : Border =>
        border.Assign(static (b, value) => b.Child = value, child);

    public static T Text<T>(this T textBlock, string text) where T : TextBlock =>
        textBlock.Assign(static (tb, value) => tb.Text = value, text);

    public static T FontSize<T>(this T textBlock, double fontSize) where T : TextBlock =>
        textBlock.Assign(static (tb, value) => tb.FontSize = value, fontSize);

    public static T Foreground<T>(this T element, Brush brush) where T : Control =>
        element.Assign(static (e, value) => e.Foreground = value, brush);

    public static T Foreground<T>(this T icon, Brush brush) where T : IconElement =>
        icon.Assign(static (e, value) => e.Foreground = value, brush);

    public static T Wrap<T>(this T textBlock, TextWrapping wrapping = TextWrapping.Wrap) where T : TextBlock =>
        textBlock.Assign(static (tb, value) => tb.TextWrapping = value, wrapping);

    public static T Content<T>(this T contentControl, object content) where T : ContentControl =>
        contentControl.Assign(static (c, value) => c.Content = value, content);

    public static T Width<T>(this T element, double width) where T : FrameworkElement =>
        element.Assign(static (e, value) => e.Width = value, width);

    public static T Height<T>(this T element, double height) where T : FrameworkElement =>
        element.Assign(static (e, value) => e.Height = value, height);
}
