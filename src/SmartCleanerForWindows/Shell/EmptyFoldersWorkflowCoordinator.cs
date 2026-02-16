using System;
using System.Collections.Generic;
using System.IO;
using SmartCleanerForWindows.Core.FileSystem;

namespace SmartCleanerForWindows.Shell;

internal sealed class EmptyFoldersWorkflowCoordinator
{
    public bool TryGetRootPath(string? rootPathText, out string root)
    {
        root = rootPathText?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    public DirectoryCleanOptions CreateOptions(
        bool dryRun,
        double depthValue,
        bool sendToRecycleBin,
        string? exclusionText,
        IReadOnlyCollection<string> inlineExcludedPaths)
    {
        int? maxDepth = null;
        if (!double.IsNaN(depthValue))
        {
            var depth = (int)Math.Max(0, Math.Round(depthValue, MidpointRounding.AwayFromZero));
            if (depth > 0)
            {
                maxDepth = depth;
            }
        }

        return new DirectoryCleanOptions
        {
            DryRun = dryRun,
            SendToRecycleBin = sendToRecycleBin,
            SkipReparsePoints = true,
            DeleteRootWhenEmpty = false,
            MaxDepth = maxDepth,
            ExcludedNamePatterns = ParseExclusions(exclusionText),
            ExcludedFullPaths = inlineExcludedPaths,
        };
    }

    public bool HasActiveFilters(string? searchText, bool hideExcluded)
        => !string.IsNullOrWhiteSpace(searchText) || hideExcluded;

    private static IReadOnlyCollection<string> ParseExclusions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
