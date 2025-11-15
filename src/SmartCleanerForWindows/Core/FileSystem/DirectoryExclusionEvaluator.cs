using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class DirectoryExclusionEvaluator : IDirectoryExclusionEvaluator
{
    private readonly HashSet<string> _fullPathExclusions;
    private readonly string[] _patterns;
    private readonly string _root;

    public DirectoryExclusionEvaluator(
        string root,
        DirectoryCleanOptions options,
        ICollection<DirectoryCleanFailure> failures)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Root path cannot be null or whitespace.", nameof(root));
        }

        _root = root;
        _fullPathExclusions = new HashSet<string>(FileSystemPathComparer.PathComparer);

        if (options.ExcludedFullPaths is { Count: > 0 })
        {
            foreach (var path in options.ExcludedFullPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                try
                {
                    var resolved = Path.IsPathRooted(path)
                        ? PathUtilities.NormalizeDirectoryPath(path)
                        : PathUtilities.NormalizeDirectoryPath(Path.Combine(root, path));
                    _fullPathExclusions.Add(resolved);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
                {
                    failures.Add(new DirectoryCleanFailure(path, ex));
                }
            }
        }

        if (options.ExcludedNamePatterns is { Count: > 0 })
        {
            var validPatterns = new List<string>();

            foreach (var pattern in options.ExcludedNamePatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                if (TryNormalizePattern(pattern, out var normalized, out var error))
                {
                    validPatterns.Add(normalized);
                }
                else if (error is not null)
                {
                    failures.Add(new DirectoryCleanFailure(pattern, error));
                }
            }

            _patterns = validPatterns.Count > 0
                ? validPatterns.ToArray()
                : [];
        }
        else
        {
            _patterns = [];
        }
    }

    public bool ShouldExclude(string path)
    {
        if (_fullPathExclusions.Contains(path))
        {
            return true;
        }

        if (_patterns.Length == 0)
        {
            return false;
        }

        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(name) && MatchesAny(_patterns, name))
        {
            return true;
        }

        var relative = Path.GetRelativePath(_root, path);
        if (!string.IsNullOrEmpty(relative) && relative is not ".")
        {
            var normalized = relative.Replace('\\', '/');
            if (MatchesAny(_patterns, normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryNormalizePattern(string pattern, out string normalized, out Exception? error)
    {
        normalized = pattern.Replace('\\', '/');

        try
        {
            _ = FileSystemName.MatchesSimpleExpression(normalized, string.Empty, FileSystemPathComparer.IgnoreCase);
            error = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = new ArgumentException($"Invalid exclusion pattern '{pattern}'.", ex);
            normalized = string.Empty;
            return false;
        }
    }

    private static bool MatchesAny(string[] patterns, string candidate)
    {
        foreach (var pattern in patterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, candidate, FileSystemPathComparer.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
