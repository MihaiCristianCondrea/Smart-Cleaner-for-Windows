using System;
using System.Text;
using Microsoft.UI.Xaml.Markup;

namespace SmartCleanerForWindows.Diagnostics;

internal static class XamlDiagnostics
{
    public static string Format(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== XAML Exception Diagnostics ===");
        sb.AppendLine(Describe(ex));

        var current = ex;
        var depth = 0;
        while (current is not null && depth++ < 10)
        {
            if (current.InnerException is null)
            {
                break;
            }

            current = current.InnerException;
            sb.AppendLine();
            sb.AppendLine($"--- Inner ({depth}) ---");
            sb.AppendLine(Describe(current));
        }

        sb.AppendLine("=== End ===");
        return sb.ToString();
    }

    private static string Describe(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Type: {ex.GetType().FullName}");
        sb.AppendLine($"Message: {ex.Message}");
        sb.AppendLine($"HResult: 0x{ex.HResult:X8}");

        if (ex is XamlParseException xamlParseException)
        {
            sb.AppendLine($"LineNumber: {xamlParseException.LineNumber}");
            sb.AppendLine($"LinePosition: {xamlParseException.LinePosition}");
        }

        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace ?? "(null)");
        return sb.ToString();
    }
}
