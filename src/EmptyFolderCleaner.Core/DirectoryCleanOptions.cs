namespace EmptyFolderCleaner.Core;

/// <summary>
/// Represents the configuration used to scan and optionally delete empty directories.
/// </summary>
public sealed record DirectoryCleanOptions
{
    /// <summary>
    /// Gets the default options which perform a dry run, skip reparse points and
    /// leave the root directory untouched.
    /// </summary>
    public static DirectoryCleanOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the cleaner should only report empty directories
    /// without deleting them.
    /// </summary>
    public bool DryRun { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether deletions should use the Windows Recycle Bin when available.
    /// Ignored when <see cref="DryRun"/> is <see langword="true"/>.
    /// </summary>
    public bool SendToRecycleBin { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether reparse points (junctions and symbolic links)
    /// should be skipped while traversing the directory tree.
    /// </summary>
    public bool SkipReparsePoints { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum depth, relative to the root directory, that will be scanned.
    /// A value of <see langword="null"/> removes the depth limit.
    /// </summary>
    public int? MaxDepth
    {
        get => _maxDepth;
        init
        {
            if (value is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Depth must be non-negative.");
            }

            _maxDepth = value;
        }
    }

    private readonly int? _maxDepth;

    /// <summary>
    /// Gets or sets a value indicating whether the root directory itself should be deleted
    /// when it becomes empty.
    /// </summary>
    public bool DeleteRootWhenEmpty { get; init; }

    /// <summary>
    /// Gets or sets the collection of simple wildcard patterns used to exclude directories by
    /// name or by relative path (using forward slashes). Examples: <c>.git</c>, <c>build/*</c>.
    /// </summary>
    public IReadOnlyCollection<string> ExcludedNamePatterns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the collection of explicit directory paths that should be skipped. Relative
    /// entries are resolved against the root directory that is cleaned.
    /// </summary>
    public IReadOnlyCollection<string> ExcludedFullPaths { get; init; } = Array.Empty<string>();
}
