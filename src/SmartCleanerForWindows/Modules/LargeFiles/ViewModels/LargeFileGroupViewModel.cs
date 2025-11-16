using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SmartCleanerForWindows.Core.Storage;

namespace SmartCleanerForWindows.Modules.LargeFiles.ViewModels;

public sealed class LargeFileGroupViewModel(
    string displayName,
    Func<int, string> formatFileCount,
    Func<long, string>? formatBytes = null)
    : INotifyPropertyChanged
{
    public string DisplayName { get; } = displayName;
    private readonly Func<int, string> _formatFileCount = formatFileCount ?? throw new ArgumentNullException(nameof(formatFileCount));
    private readonly Func<long, string> _formatBytes = formatBytes ?? ValueFormatting.FormatBytes;
    private long _totalBytes;

    private ObservableCollection<LargeFileItemViewModel> Items { get; } = [];

    public long TotalBytes => _totalBytes;

    public int ItemCount => Items.Count;

    public string Summary => string.Format(
        CultureInfo.CurrentCulture,
        "{0} • {1}",
        _formatBytes(Math.Max(0L, _totalBytes)),
        _formatFileCount(ItemCount));

    public event PropertyChangedEventHandler? PropertyChanged;

    public void AddItem(LargeFileItemViewModel item)
    {
        Items.Add(item);
        _totalBytes += item.Size;
        OnGroupChanged();
    }

    public bool RemoveItem(LargeFileItemViewModel item)
    {
        if (Items.Remove(item))
        {
            _totalBytes -= item.Size;
            OnGroupChanged();
            return true;
        }

        return false;
    }

    private void OnGroupChanged()
    {
        OnPropertyChanged(nameof(TotalBytes));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(Summary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
