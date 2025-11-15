using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;
using Windows.ApplicationModel;

namespace Smart_Cleaner_for_Windows;

public abstract class Program
{
    private const uint WindowsAppSdkMajorMinor = 0x00010008;

    [STAThread]
    public static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        var isPackaged = IsRunningPackaged();
        var bootstrapInitialized = false;

        if (!isPackaged)
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            bootstrapInitialized = true;
        }

        try
        {
            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new Shell.App();
            });
        }
        finally
        {
            if (bootstrapInitialized)
            {
                Bootstrap.Shutdown();
            }
        }
    }

    private static bool IsRunningPackaged()
    {
        try
        {
            return Package.Current is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
