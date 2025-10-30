using Microsoft.UI.Xaml;

namespace Smart_Cleaner_for_Windows.Shell;

public partial class App : Application // FIXME: Base type 'Application' is already specified in other parts
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
