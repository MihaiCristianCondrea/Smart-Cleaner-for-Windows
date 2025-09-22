using System.IO.Enumeration;
using System.Runtime.Versioning;
using Microsoft.VisualBasic.FileIO;

namespace EmptyFolderCleaner.Core;

/// <summary>
/// Provides helpers to identify and remove empty directories.
/// </summary>
public static class DirectoryCleaner
{
    private static readonly bool IgnoreCase = OperatingSystem.IsWindows();
    private static readonly StringComparer PathComparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Scans the directory tree rooted at <paramref name="root"/> and optionally deletes empty directories.
    /// </summary>
    /// <param name="root">Root directory to scan.</param>
    /// <param name="options">Configuration that controls traversal and deletion behavior.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result of the cleaning operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    public static DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentNullException(nameof(root));
        }

        options ??= DirectoryCleanOptions.Default;

        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"The directory '{root}' does not exist.");
        }

        root = NormalizePath(root);

        var empty = new List<string>();
        var deleted = new List<string>();
        var failures = new List<DirectoryCleanFailure>();
        var exclusions = new DirectoryExclusionFilter(root, options);

        foreach (var directory in EnumerateBottomUp(root, options, exclusions, failures, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(directory))
            {
                continue;
            }

            if (!options.DeleteRootWhenEmpty && PathComparer.Equals(directory, root))
            {
                continue;
            }

            if (!IsDirectoryEmpty(directory, failures))
            {
                continue;
            }

            empty.Add(directory);

            if (options.DryRun)
            {
                continue;
            }

            try
            {
                DeleteDirectory(directory, options.SendToRecycleBin);
                deleted.Add(directory);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add(new DirectoryCleanFailure(directory, ex));
            }
            catch (Exception ex)
            {
                failures.Add(new DirectoryCleanFailure(directory, ex));
            }
        }

        return new DirectoryCleanResult(empty, deleted, failures);
    }

    private static IEnumerable<string> EnumerateBottomUp(
        string root,
        DirectoryCleanOptions options,
        DirectoryExclusionFilter exclusions,
        ICollection<DirectoryCleanFailure> failures,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<(string Path, int Depth)>();
        var ordered = new Stack<string>();
        pending.Push((root, 0));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (current, depth) = pending.Pop();
            ordered.Push(current);

            if (options.MaxDepth is { } maxDepth && depth >= maxDepth)
            {
                continue;
            }

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add(new DirectoryCleanFailure(current, ex));
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalized = NormalizePath(child);

                if (exclusions.ShouldExclude(normalized))
                {
                    continue;
                }

                if (options.SkipReparsePoints && IsReparsePoint(normalized, failures))
                {
                    continue;
                }

                pending.Push((normalized, depth + 1));
            }
        }

        while (ordered.Count > 0)
        {
            yield return ordered.Pop();
        }
    }

    private static bool IsDirectoryEmpty(string directory, ICollection<DirectoryCleanFailure> failures)
    {
        try
        {
            using var enumerator = Directory.EnumerateFileSystemEntries(directory).GetEnumerator();
            return !enumerator.MoveNext();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(directory, ex));
            return false;
        }
    }

    private static bool IsReparsePoint(string path, ICollection<DirectoryCleanFailure> failures)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(path, ex));
            return true;
        }
    }

    private static void DeleteDirectory(string path, bool sendToRecycleBin)
    {
        if (sendToRecycleBin)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Deleting to the Recycle Bin is only supported on Windows.");
            }

            DeleteToRecycleBin(path);
            return;
        }

        Directory.Delete(path, recursive: false);
    }

    [SupportedOSPlatform("windows")]
    private static void DeleteToRecycleBin(string path)
    {
        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private sealed class DirectoryExclusionFilter
    {
        private readonly HashSet<string> _fullPathExclusions;
        private readonly string[] _patterns;
        private readonly string _root;

        public DirectoryExclusionFilter(string root, DirectoryCleanOptions options)
        {
            _root = root;
            _fullPathExclusions = new HashSet<string>(PathComparer);

            if (options.ExcludedFullPaths is { Count: > 0 })
            {
                foreach (var path in options.ExcludedFullPaths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    var resolved = Path.IsPathRooted(path)
                        ? NormalizePath(path)
                        : NormalizePath(Path.Combine(root, path));
                    _fullPathExclusions.Add(resolved);
                }
            }

            _patterns = options.ExcludedNamePatterns is { Count: > 0 }
                ? options.ExcludedNamePatterns
                    .Where(static pattern => !string.IsNullOrWhiteSpace(pattern))
                    .Select(static pattern => pattern.Replace('\\', '/'))
                    .ToArray()
                : Array.Empty<string>();
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

        private static bool MatchesAny(string[] patterns, string candidate)
        {
            foreach (var pattern in patterns)
            {
                if (FileSystemName.MatchesSimpleExpression(pattern, candidate, IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
