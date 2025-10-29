using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.InternetRepair.ViewModels;

public sealed class InternetRepairLogEntry : INotifyPropertyChanged
{
    private Symbol _icon;
    private string _description;

    public InternetRepairLogEntry(string title)
    {
        Title = title;
        _description = string.Empty;
        _icon = Symbol.Sync;
    }

    public string Title { get; }

    public Symbol Icon
    {
        get => _icon;
        private set
        {
            if (_icon != value)
            {
                _icon = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        private set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(Symbol icon, string description)
    {
        Icon = icon;
        Description = description;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
