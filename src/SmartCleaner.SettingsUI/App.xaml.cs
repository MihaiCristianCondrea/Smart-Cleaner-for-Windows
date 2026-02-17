using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartCleanerForWindows.SettingsUi;

public sealed class App : Application
{
    private Window? _window;

    public App()
    {
        Resources.MergedDictionaries.Add(new XamlControlsResources());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        _window = new MainWindow();
        _window.Activate();
    }
}
