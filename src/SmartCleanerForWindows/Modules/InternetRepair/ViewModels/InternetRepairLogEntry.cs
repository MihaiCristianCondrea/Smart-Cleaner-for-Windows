using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Smart_Cleaner_for_Windows.Modules.InternetRepair.ViewModels;

public sealed class InternetRepairLogEntry(string title) : INotifyPropertyChanged
{
    private Symbol _icon = Symbol.Sync;
    private string _description = string.Empty;

    public string Title { get; } = title;

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
