using System;
using Microsoft.UI.Xaml;
using Serilog;

namespace SmartCleanerForWindows.Diagnostics;

internal static class UiConstructionLog
{
    public static T Create<T>(Func<T> factory, string name)
    {
        Log.Information("UI: constructing {Name} ({Type})", name, typeof(T).FullName);
        try
        {
            var value = factory();
            Log.Information("UI: constructed OK: {Name}", name);
            return value;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UI: FAILED constructing {Name} ({Type})", name, typeof(T).FullName);
            throw;
        }
    }

    public static void AttachFrameworkElementDiagnostics(FrameworkElement frameworkElement, string name)
    {
        frameworkElement.Loaded += (_, _) =>
            Log.Information("UI: Loaded {Name} ({Type})", name, frameworkElement.GetType().FullName);
        frameworkElement.Unloaded += (_, _) =>
            Log.Information("UI: Unloaded {Name} ({Type})", name, frameworkElement.GetType().FullName);
    }
}
