using Microsoft.UI.Xaml;

namespace SmartCleanerForWindows.SettingsUi;

public partial class App : Application
{
    private Window? _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        _window = new ToolSettingsWindow();
        _window.Activate();
    }
}
