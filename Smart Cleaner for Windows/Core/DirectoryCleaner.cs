using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Provides helpers to identify and remove empty directories.
/// </summary>
public sealed class DirectoryCleaner : IDirectoryCleaner
{
    private static readonly bool IgnoreCase = OperatingSystem.IsWindows();
    private static readonly StringComparer PathComparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private readonly IDirectorySystem _directorySystem;
    private readonly IDirectoryDeleter _directoryDeleter;

    public static IDirectoryCleaner Default { get; } = new DirectoryCleaner();

    public static DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Default.Clean(root, options, cancellationToken);
    }

    public DirectoryCleaner()
        : this(new FileSystemDirectorySystem(), new FileSystemDirectoryDeleter())
    {
    }

    public DirectoryCleaner(IDirectorySystem directorySystem, IDirectoryDeleter directoryDeleter)
    {
        _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem));
        _directoryDeleter = directoryDeleter ?? throw new ArgumentNullException(nameof(directoryDeleter));
    }

    DirectoryCleanResult IDirectoryCleaner.Clean(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
    {
        return CleanInternal(root, options, cancellationToken);
    }

    public Task<DirectoryCleanResult> CleanAsync(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => CleanInternal(root, options, cancellationToken), cancellationToken);
    }

    Task<DirectoryCleanResult> IDirectoryCleaner.CleanAsync(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
    {
        return CleanAsync(root, options, cancellationToken);
    }

    /// <summary>
    /// Scans the directory tree rooted at <paramref name="root"/> and optionally deletes empty directories.
    /// </summary>
    /// <param name="root">Root directory to scan.</param>
    /// <param name="options">Configuration that controls traversal and deletion behavior.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result of the cleaning operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the root directory does not exist.</exception>
    private DirectoryCleanResult CleanInternal(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentNullException(nameof(root));
        }

        options ??= DirectoryCleanOptions.Default;

        if (!_directorySystem.Exists(root))
        {
            throw new DirectoryNotFoundException($"The directory '{root}' does not exist.");
        }

        root = NormalizePath(root);

        var empty = new List<string>();
        var deleted = new List<string>();
        var failures = new List<DirectoryCleanFailure>();
        var exclusions = new DirectoryExclusionFilter(root, options, failures);

        foreach (var directory in EnumerateBottomUp(root, options, exclusions, failures, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_directorySystem.Exists(directory))
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
                var mode = options.SendToRecycleBin
                    ? DirectoryDeletionMode.RecycleBin
                    : DirectoryDeletionMode.Permanent;
                _directoryDeleter.Delete(directory, mode);
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

    private IEnumerable<string> EnumerateBottomUp(
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
                children = _directorySystem.EnumerateDirectories(current);
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

    private bool IsDirectoryEmpty(string directory, ICollection<DirectoryCleanFailure> failures)
    {
        try
        {
            using var enumerator = _directorySystem.EnumerateFileSystemEntries(directory).GetEnumerator();
            return !enumerator.MoveNext();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(directory, ex));
            return false;
        }
    }

    private bool IsReparsePoint(string path, ICollection<DirectoryCleanFailure> failures)
    {
        try
        {
            var attributes = _directorySystem.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(new DirectoryCleanFailure(path, ex));
            return true;
        }
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

        public DirectoryExclusionFilter(string root, DirectoryCleanOptions options, ICollection<DirectoryCleanFailure> failures)
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

                    try
                    {
                        var resolved = Path.IsPathRooted(path)
                            ? NormalizePath(path)
                            : NormalizePath(Path.Combine(root, path));
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
                    : Array.Empty<string>();
            }
            else
            {
                _patterns = Array.Empty<string>();
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
                _ = FileSystemName.MatchesSimpleExpression(normalized, string.Empty, IgnoreCase);
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
                if (FileSystemName.MatchesSimpleExpression(pattern, candidate, IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
