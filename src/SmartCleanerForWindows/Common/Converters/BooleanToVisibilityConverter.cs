using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SmartCleanerForWindows.Common.Converters;

/// <summary>
/// Converts boolean values to <see cref="Visibility"/> instances.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets a value indicating whether the conversion should be inverted.
    /// </summary>
    public bool IsInverted { get; set; }

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is bool b && b;

        if (ShouldInvert(parameter))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;

            if (ShouldInvert(parameter))
            {
                result = !result;
            }

            return result;
        }

        return false;
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
