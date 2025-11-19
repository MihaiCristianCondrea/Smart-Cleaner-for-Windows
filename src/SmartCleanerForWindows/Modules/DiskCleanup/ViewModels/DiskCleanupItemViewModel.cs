using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartCleanerForWindows.Core.DiskCleanup;
using SmartCleanerForWindows.Core.Storage;

namespace SmartCleanerForWindows.Modules.DiskCleanup.ViewModels;

public sealed class DiskCleanupItemViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public DiskCleanupItemViewModel(DiskCleanupItem item)
    {
        Item = item;

        if (item.CanSelect && (item.Flags.HasFlag(DiskCleanupFlags.RunByDefault) ||
                               item.Flags.HasFlag(DiskCleanupFlags.EnableByDefault)))
        {
            _isSelected = true;
        }
    }

    public DiskCleanupItem Item { get; }

    public string Name => Item.Name;

    public string? Description => Item.Description;

    public string FormattedSize => ValueFormatting.FormatBytes(Item.Size);

    public bool CanSelect => Item.CanSelect;

    public string? ErrorMessage => Item.Error;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DiskCleanupItemViewModel() : this(new DiskCleanupItem())
    {
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
