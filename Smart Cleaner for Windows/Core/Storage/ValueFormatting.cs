using System.Globalization;

namespace Smart_Cleaner_for_Windows.Core.Storage;

public static class ValueFormatting
{
    private static readonly string[] s_suffixes =
    {
        "B",
        "KB",
        "MB",
        "GB",
        "TB",
        "PB",
        "EB",
    };

    public static string FormatBytes(long value)
    {
        if (value <= 0)
        {
            return "0 B";
        }

        return FormatBytes((ulong)value);
    }

    public static string FormatBytes(ulong value)
    {
        if (value == 0)
        {
            return "0 B";
        }

        double size = value;
        var index = 0;

        while (size >= 1024 && index < s_suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:0.##} {1}", size, s_suffixes[index]);
    }
}
