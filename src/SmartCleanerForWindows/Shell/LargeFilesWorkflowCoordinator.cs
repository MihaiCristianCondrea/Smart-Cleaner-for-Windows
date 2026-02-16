using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml.Controls;
using SmartCleanerForWindows.Core.LargeFiles;

namespace SmartCleanerForWindows.Shell;

internal sealed class LargeFilesWorkflowCoordinator
{
    private readonly ILargeFilesWorkflowView _view;
    private readonly StringComparer _pathComparer;
    private readonly HashSet<string> _exclusionLookup;

    public LargeFilesWorkflowCoordinator(ILargeFilesWorkflowView view, StringComparer? pathComparer = null)
    {
        _view = view;
        _pathComparer = pathComparer ?? (OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        _exclusionLookup = new HashSet<string>(_pathComparer);
    }

    public ObservableCollection<string> Exclusions { get; } = [];

    public bool TryGetRootPath(string? text, out string root)
    {
        root = text?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    public LargeFileScanOptions CreateOptions(bool includeSubfolders, double maxItemsValue, string? exclusionPatterns)
    {
        var maxItems = 100;
        if (!double.IsNaN(maxItemsValue))
        {
            maxItems = (int)Math.Max(1, Math.Round(maxItemsValue, MidpointRounding.AwayFromZero));
        }

        return new LargeFileScanOptions
        {
            IncludeSubdirectories = includeSubfolders,
            SkipReparsePoints = true,
            MaxResults = maxItems,
            ExcludedNamePatterns = ParseExclusions(exclusionPatterns),
            ExcludedFullPaths = Exclusions.ToList(),
        };
    }

    public void LoadPreferences(JsonObject values, double fallbackMaxItems, Action<int> applyMaxItems)
    {
        if (values.TryGetPropertyValue("excludedPaths", out var exclusionsNode))
        {
            var exclusions = exclusionsNode?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(exclusions))
            {
                foreach (var exclusion in exclusions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    TryAddExclusion(exclusion, showMessageOnError: false);
                }
            }
        }

        if (values.TryGetPropertyValue("minimumSizeMb", out var minimumSizeNode))
        {
            var maxItems = minimumSizeNode switch
            {
                JsonValue value when value.TryGetValue<double>(out var sizeValue) => Math.Clamp((int)Math.Round(sizeValue, MidpointRounding.AwayFromZero), 1, 5000),
                JsonValue value when value.TryGetValue<int>(out var sizeValue) => Math.Clamp(sizeValue, 1, 5000),
                _ => (int)fallbackMaxItems
            };

            applyMaxItems(maxItems);
        }

        UpdateExclusionState();
    }

    public bool TryAddExclusion(string path, bool showMessageOnError = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var normalized = NormalizePath(path);
            if (!_exclusionLookup.Add(normalized))
            {
                if (showMessageOnError)
                {
                    _view.ShowLargeFilesInfoMessage("That file is already excluded.", InfoBarSeverity.Informational);
                }

                return false;
            }

            Exclusions.Add(normalized);
            UpdateExclusionState();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            if (showMessageOnError)
            {
                _view.ShowLargeFilesInfoMessage(string.Format(CultureInfo.CurrentCulture, "Couldn't add exclusion: {0}", ex.Message), InfoBarSeverity.Error);
            }

            return false;
        }
    }

    public void RemoveExclusion(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalized;
        try
        {
            normalized = NormalizePath(path);
        }
        catch
        {
            normalized = path;
        }

        for (var i = Exclusions.Count - 1; i >= 0; i--)
        {
            if (!_pathComparer.Equals(Exclusions[i], normalized))
            {
                continue;
            }

            Exclusions.RemoveAt(i);
            break;
        }

        _exclusionLookup.Remove(normalized);
        UpdateExclusionState();
    }

    public void ClearExclusions()
    {
        Exclusions.Clear();
        _exclusionLookup.Clear();
        UpdateExclusionState();
    }

    public void SetExclusions(IEnumerable<string> values)
    {
        Exclusions.Clear();
        _exclusionLookup.Clear();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = NormalizePath(value);
            if (!_exclusionLookup.Add(normalized))
            {
                continue;
            }

            Exclusions.Add(normalized);
        }

        UpdateExclusionState();
    }

    private void UpdateExclusionState() => _view.SetLargeFilesExclusionState(Exclusions.Count > 0);

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private static IReadOnlyCollection<string> ParseExclusions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
