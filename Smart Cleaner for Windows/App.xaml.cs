using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Smart_Cleaner_for_Windows.Modules.Dashboard;
using Smart_Cleaner_for_Windows.Modules.DiskCleanup;
using Smart_Cleaner_for_Windows.Modules.EmptyFolders;
using Smart_Cleaner_for_Windows.Modules.LargeFiles;
using Smart_Cleaner_for_Windows.Modules.Settings;
using Smart_Cleaner_for_Windows.Shell;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows;

public partial class App
{
    private readonly IHost _host;

    public App()
    {
        InitializeComponent();
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host.Start();
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<NavigationHostViewModel>();

        services.AddSingleton<INavigationModule, DashboardModule>();
        services.AddSingleton<INavigationModule, EmptyFoldersModule>();
        services.AddSingleton<INavigationModule, LargeFilesModule>();
        services.AddSingleton<INavigationModule, DiskCleanupModule>();
        services.AddSingleton<INavigationModule, SettingsModule>();

        services.AddTransient<DashboardView>();
        services.AddTransient<EmptyFoldersView>();
        services.AddTransient<LargeFilesView>();
        services.AddTransient<DiskCleanupView>();
        services.AddTransient<SettingsView>();
    }
}
