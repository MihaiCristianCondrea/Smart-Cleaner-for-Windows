using System;
using Microsoft.UI.Xaml;

namespace EmptyFolderCleaner.WinUI;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
