using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SmartCleanerForWindows.Common.Converters;

/// <summary>
/// Converts boolean values to <see cref="Visibility"/> instances.
/// </summary>
public sealed class BooleanToVisibilityConverter(bool isInverted) : IValueConverter
{
    /// <summary>
    /// Gets or sets a value indicating whether the conversion should be inverted.
    /// </summary>
    private bool IsInverted { get; set; } = isInverted;

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is true;

        if (ShouldInvert(parameter))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is not Visibility visibility) return false;
        var result = visibility == Visibility.Visible;

        if (ShouldInvert(parameter))
        {
            result = !result;
        }

        return result;

    }

    private bool ShouldInvert(object parameter)
    {
        var invert = IsInverted;

        if (parameter is string parameterString && !string.IsNullOrWhiteSpace(parameterString))
        {
            if (bool.TryParse(parameterString, out var parsedBool))
            {
                invert ^= parsedBool;
            }
            else if (parameterString.Equals("invert", StringComparison.OrdinalIgnoreCase) ||
                     parameterString.Equals("inverse", StringComparison.OrdinalIgnoreCase))
            {
                invert = !invert;
            }
        }

        return invert;
    }
}
