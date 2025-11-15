using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Smart_Cleaner_for_Windows.Modules.EmptyFolders.ViewModels;

/// <summary>
/// Represents an empty folder candidate displayed in the tree view, including
/// its hierarchical relationship and exclusion state.
/// </summary>
public sealed class EmptyFolderNode(string name, string fullPath, string relativePath, int depth)
    : INotifyPropertyChanged
{
    // ViewModel-safe severity (map to UI severity in XAML via a converter)
    public enum PathSeverity
    {
        None = 0,
        Warning = 1,
        Error = 2
    }

    private readonly List<EmptyFolderNode> _allChildren = new();
    private readonly ObservableCollection<EmptyFolderNode> _visibleChildren = new();
    private bool _isDirectlyExcluded;
    private bool _hasExcludedAncestor;
    private bool _isVisible = true;
    private bool _isSearchMatch;

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public string FullPath { get; } = fullPath ?? throw new ArgumentNullException(nameof(fullPath));

    public string RelativePath { get; } = relativePath ?? throw new ArgumentNullException(nameof(relativePath));

    public int Depth { get; } = depth;

    public int PathLength { get; } = (fullPath ?? throw new ArgumentNullException(nameof(fullPath))).Length;

    public bool IsPathLengthWarning => PathLength >= PathLengthWarningThreshold;

    private bool IsPathLengthCritical => PathLength >= PathLengthCriticalThreshold;

    // Decoupled from Microsoft.UI types so this compiles in ViewModel layer
    public PathSeverity PathLengthSeverity => IsPathLengthCritical ? PathSeverity.Error : PathSeverity.Warning;

    public string PathLengthIndicatorTooltip => IsPathLengthCritical
        ? string.Format(
            CultureInfo.CurrentCulture,
            "Path length {0} meets or exceeds the Windows MAX_PATH limit ({1}).",
            PathLength,
            PathLengthCriticalThreshold)
        : string.Format(
            CultureInfo.CurrentCulture,
            "Path length {0}. Paths longer than {1} characters might fail to delete.",
            PathLength,
            PathLengthCriticalThreshold);

    public EmptyFolderNode? Parent { get; private set; }

    public ObservableCollection<EmptyFolderNode> Children => _visibleChildren;

    public IReadOnlyList<EmptyFolderNode> AllChildren => _allChildren;

    public bool HasVisibleChildren => _visibleChildren.Count > 0;

    public bool HasExcludedAncestor => _hasExcludedAncestor;

    public bool IsDirectlyExcluded
    {
        get => _isDirectlyExcluded;
        set
        {
            if (_isDirectlyExcluded == value)
            {
                return;
            }

            _isDirectlyExcluded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEffectivelyExcluded));
            OnPropertyChanged(nameof(IsInlineToggleEnabled));

            foreach (var child in _allChildren)
            {
                child.UpdateAncestorExclusion(IsEffectivelyExcluded);
            }

            ExclusionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsEffectivelyExcluded => _isDirectlyExcluded || _hasExcludedAncestor;

    public bool IsInlineToggleEnabled => !_hasExcludedAncestor;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsSearchMatch
    {
        get => _isSearchMatch;
        set
        {
            if (_isSearchMatch == value)
            {
                return;
            }

            _isSearchMatch = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? ExclusionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private const int PathLengthWarningThreshold = 200;
    private const int PathLengthCriticalThreshold = 260;

    public void AddChild(EmptyFolderNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_allChildren.Contains(child))
        {
            return;
        }

        child.Parent = this;
        _allChildren.Add(child);
        _visibleChildren.Add(child);
        child.UpdateAncestorExclusion(IsEffectivelyExcluded);
        OnPropertyChanged(nameof(HasVisibleChildren));
    }

    public void UpdateVisibleChildren(IList<EmptyFolderNode> visibleChildren)
    {
        _visibleChildren.Clear();

        foreach (var child in visibleChildren)
        {
            _visibleChildren.Add(child);
        }

        OnPropertyChanged(nameof(HasVisibleChildren));
    }

    public void ResetVisibleChildren()
    {
        UpdateVisibleChildren(_allChildren);
    }

    public void SortChildren(Comparison<EmptyFolderNode> comparison)
    {
        if (_allChildren.Count <= 1)
        {
            return;
        }

        _allChildren.Sort(comparison);

        var currentlyVisible = new List<EmptyFolderNode>();
        foreach (var child in _allChildren)
        {
            if (child.IsVisible)
            {
                currentlyVisible.Add(child);
            }
        }

        UpdateVisibleChildren(currentlyVisible);
    }

    public IEnumerable<EmptyFolderNode> EnumerateSelfAndDescendants()
    {
        yield return this;

        foreach (var child in _allChildren)
        {
            foreach (var descendant in child.EnumerateSelfAndDescendants())
            {
                yield return descendant;
            }
        }
    }

    private void UpdateAncestorExclusion(bool ancestorExcluded)
    {
        if (_hasExcludedAncestor == ancestorExcluded)
        {
            return;
        }

        _hasExcludedAncestor = ancestorExcluded;
        OnPropertyChanged(nameof(HasExcludedAncestor));
        OnPropertyChanged(nameof(IsEffectivelyExcluded));
        OnPropertyChanged(nameof(IsInlineToggleEnabled));

        foreach (var child in _allChildren)
        {
            child.UpdateAncestorExclusion(IsEffectivelyExcluded);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}