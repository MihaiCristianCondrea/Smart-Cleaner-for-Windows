using System;
using Microsoft.UI.Xaml.Data;

namespace Smart_Cleaner_for_Windows.Shell;

/// <summary>
/// Converts boolean values into opacity levels for preview items.
/// </summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the opacity value returned when the boolean input is <c>true</c>.
    /// </summary>
    public double TrueOpacity { get; set; } = 0.35;

    /// <summary>
    /// Gets or sets the opacity value returned when the boolean input is <c>false</c>.
    /// </summary>
    public double FalseOpacity { get; set; } = 1.0;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool flag)
        {
            return flag ? TrueOpacity : FalseOpacity;
        }

        return FalseOpacity;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("Opacity conversion is one-way only.");
    }
}
