using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Smart_Cleaner_for_Windows.Core.DiskCleanup;
using Smart_Cleaner_for_Windows.Core.LargeFiles;

namespace Smart_Cleaner_for_Windows;

public sealed partial class MainWindow
{
    private sealed class DriveUsageViewModel
    {
        public DriveUsageViewModel(string name, string details, double usedPercentage, string usageSummary)
        {
            Name = name;
            Details = details;
            UsedPercentage = usedPercentage;
            UsageSummary = usageSummary;
        }

        public string Name { get; }

        public string Details { get; }

        public double UsedPercentage { get; }

        public string UsageSummary { get; }
    }

    private sealed class LargeFileGroupViewModel : INotifyPropertyChanged
    {
        private readonly MainWindow _owner;
        private long _totalBytes;

        public LargeFileGroupViewModel(MainWindow owner, string displayName)
        {
            _owner = owner;
            DisplayName = displayName;
            Items = new ObservableCollection<LargeFileItemViewModel>();
        }

        public string DisplayName { get; }

        public ObservableCollection<LargeFileItemViewModel> Items { get; }

        public long TotalBytes => _totalBytes;

        public int ItemCount => Items.Count;

        public string Summary => string.Format(
            CultureInfo.CurrentCulture,
            "{0} • {1}",
            FormatBytes((ulong)Math.Max(0L, _totalBytes)),
            _owner.FormatFileCount(ItemCount));

        public void AddItem(LargeFileItemViewModel item)
        {
            Items.Add(item);
            _totalBytes += item.Size;
            OnPropertyChanged(nameof(TotalBytes));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(Summary));
        }

        public bool RemoveItem(LargeFileItemViewModel item)
        {
            if (Items.Remove(item))
            {
                _totalBytes -= item.Size;
                OnPropertyChanged(nameof(TotalBytes));
                OnPropertyChanged(nameof(ItemCount));
                OnPropertyChanged(nameof(Summary));
                return true;
            }

            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private sealed class LargeFileItemViewModel
    {
        public LargeFileItemViewModel(LargeFileEntry entry, string extensionDisplay)
        {
            Entry = entry;
            Path = entry.Path;
            Name = entry.Name;
            Directory = string.IsNullOrEmpty(entry.Directory) ? entry.Path : entry.Directory;
            Size = entry.Size;
            SizeDisplay = FormatBytes((ulong)Math.Max(0L, entry.Size));
            TypeName = entry.Type;
            ExtensionDisplay = extensionDisplay;
        }

        public LargeFileEntry Entry { get; }

        public string Path { get; }

        public string Name { get; }

        public string Directory { get; }

        public long Size { get; }

        public string SizeDisplay { get; }

        public string TypeName { get; }

        public string ExtensionDisplay { get; }
    }

    private sealed class DiskCleanupItemViewModel : INotifyPropertyChanged
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

        internal DiskCleanupItem Item { get; }

        public string Name => Item.Name;

        public string? Description => Item.Description;

        public string FormattedSize => FormatBytes(Item.Size);

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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

