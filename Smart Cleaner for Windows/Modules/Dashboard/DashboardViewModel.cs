using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Smart_Cleaner_for_Windows.Modules.Dashboard;

public sealed class DashboardViewModel
{
    public DashboardViewModel()
    {
        // Placeholder data until storage services are refactored into modules.
        DriveUsage.Add(new DriveUsageItemViewModel("System", "C:", 58));
        DriveUsage.Add(new DriveUsageItemViewModel("Data", "D:", 12));

        NavigateToEmptyFoldersCommand = new RelayCommand(static _ => { });
    }

    public ObservableCollection<DriveUsageItemViewModel> DriveUsage { get; } = new();

    public ICommand NavigateToEmptyFoldersCommand { get; }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}

public sealed record DriveUsageItemViewModel(string Name, string UsageSummary, double UsedPercentage)
{
    public string Details => $"{UsageSummary} drive";
}
