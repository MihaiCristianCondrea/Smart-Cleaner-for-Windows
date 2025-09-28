using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Smart_Cleaner_for_Windows.Shell.Navigation;

namespace Smart_Cleaner_for_Windows.Shell;

public sealed partial class MainWindow : Window
{
    private readonly NavigationHostViewModel _viewModel;
    private readonly IServiceProvider _services;

    public MainWindow(NavigationHostViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        DataContext = _viewModel;

        if (_viewModel.SelectedModule is not null)
        {
            RootNavigation.SelectedItem = _viewModel.SelectedModule;
            LoadModule(_viewModel.SelectedModule);
        }
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var module = ResolveModule(args.SelectedItem, args.SelectedItemContainer);
        if (module is null || module == _viewModel.SelectedModule)
        {
            return;
        }

        _viewModel.SelectedModule = module;
        LoadModule(module);
    }

    private INavigationModule? ResolveModule(object? selectedItem, NavigationViewItem? container)
    {
        if (selectedItem is INavigationModule moduleFromItem)
        {
            return moduleFromItem;
        }

        if (selectedItem is NavigationViewItem item && item.Tag is INavigationModule moduleFromTag)
        {
            return moduleFromTag;
        }

        if (container?.Tag is INavigationModule moduleFromContainer)
        {
            return moduleFromContainer;
        }

        return null;
    }

    private void LoadModule(INavigationModule module)
    {
        var view = _services.GetRequiredService(module.ViewType) as FrameworkElement;
        ModuleContent.Content = view;
    }
}
