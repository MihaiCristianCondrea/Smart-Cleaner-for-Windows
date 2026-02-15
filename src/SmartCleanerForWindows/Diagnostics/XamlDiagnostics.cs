using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Markup;

namespace SmartCleanerForWindows.Diagnostics;

internal static partial class XamlDiagnostics
{
    private const int MaxInnerDepth = 10;

    [GeneratedRegex(@"\[\s*Line:\s*(\d+)\s*Position:\s*(\d+)\s*\]", RegexOptions.CultureInvariant)]
    private static partial Regex WinUiLinePosRegex();

    [GeneratedRegex(@"Line number\s*'(\d+)'\s*and\s*line position\s*'(\d+)'",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex WpfLinePosRegex();

    public static string Format(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var sb = new StringBuilder(capacity: 1024);
        sb.AppendLine("=== XAML Exception Diagnostics ===");

        AppendException(sb, ex);

        var current = ex.InnerException;
        for (var depth = 1; current is not null && depth <= MaxInnerDepth; depth++)
        {
            sb.AppendLine();
            sb.AppendLine($"--- Inner ({depth}) ---");
            AppendException(sb, current);
            current = current.InnerException;
        }

        sb.AppendLine("=== End ===");
        return sb.ToString();
    }

    private static void AppendException(StringBuilder sb, Exception ex)
    {
        sb.AppendLine($"Type: {ex.GetType().FullName}");
        sb.AppendLine($"Message: {ex.Message}");
        sb.AppendLine($"HResult: 0x{ex.HResult:X8}");

        if (ex is System.IO.FileNotFoundException fileNotFound)
        {
            if (!string.IsNullOrWhiteSpace(fileNotFound.FileName))
            {
                sb.AppendLine($"FileName: {fileNotFound.FileName}");
            }

            if (!string.IsNullOrWhiteSpace(fileNotFound.FusionLog))
            {
                sb.AppendLine($"FusionLog: {fileNotFound.FusionLog}");
            }
        }

        if (!string.IsNullOrWhiteSpace(ex.Source))
        {
            sb.AppendLine($"Source: {ex.Source}");
        }

        if (TryGetXamlLineInfo(ex, out var line, out var position))
        {
            sb.AppendLine($"XamlLine: {line}, XamlPosition: {position}");
        }

        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace ?? "(null)");
    }

    private static bool TryGetXamlLineInfo(Exception ex, out int line, out int position)
    {
        if (TryGetIntProperty(ex, "LineNumber", out line) && TryGetIntProperty(ex, "LinePosition", out position))
        {
            return true;
        }

        if (TryGetIntProperty(ex, "Line", out line) && TryGetIntProperty(ex, "Column", out position))
        {
            return true;
        }

        var msg = ex.Message;
        if (!string.IsNullOrEmpty(msg))
        {
            if (TryParseFromRegex(WinUiLinePosRegex(), msg, out line, out position) ||
                TryParseFromRegex(WpfLinePosRegex(), msg, out line, out position))
            {
                return true;
            }
        }

        line = 0;
        position = 0;
        return false;
    }

    private static bool TryGetIntProperty(object instance, string propertyName, out int value)
    {
        value = 0;

        var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (prop is null || !prop.CanRead)
        {
            return false;
        }

        var raw = prop.GetValue(instance);
        if (raw is null)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseFromRegex(Regex regex, string message, out int line, out int position)
    {
        line = 0;
        position = 0;

        var m = regex.Match(message);
        if (!m.Success || m.Groups.Count < 3)
        {
            return false;
        }

        return int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out line) &&
               int.TryParse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out position);
    }
}
