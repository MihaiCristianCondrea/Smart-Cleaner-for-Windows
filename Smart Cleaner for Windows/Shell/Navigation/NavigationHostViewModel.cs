using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Smart_Cleaner_for_Windows.Shell.Navigation;

public sealed class NavigationHostViewModel : INotifyPropertyChanged
{
    private INavigationModule? _selectedModule;

    public NavigationHostViewModel(IEnumerable<INavigationModule> modules)
    {
        Modules = new ObservableCollection<INavigationModule>(modules
            .OrderBy(module => module.Order)
            .ThenBy(module => module.Title));
        _selectedModule = Modules.FirstOrDefault();
    }

    public ObservableCollection<INavigationModule> Modules { get; }

    public INavigationModule? SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (!EqualityComparer<INavigationModule?>.Default.Equals(_selectedModule, value))
            {
                _selectedModule = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedModule)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
