using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Smart_Cleaner_for_Windows.Core.FileSystem;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Provides helpers to identify and remove empty directories.
/// </summary>
public sealed class DirectoryCleaner : IDirectoryCleaner
{
    private readonly IDirectorySystem _directorySystem;
    private readonly IDirectoryTraversalService _traversalService;
    private readonly IEmptyDirectoryDetector _emptyDirectoryDetector;
    private readonly IDirectoryDeletionService _directoryDeletionService;

    public static IDirectoryCleaner Default { get; } = new DirectoryCleaner();

    private DirectoryCleaner()
        : this(new FileSystemDirectorySystem(), new FileSystemDirectoryDeleter())
    {
    }

    public DirectoryCleaner(
        IDirectorySystem directorySystem,
        IDirectoryDeleter directoryDeleter,
        IDirectoryTraversalService? traversalService = null,
        IEmptyDirectoryDetector? emptyDirectoryDetector = null,
        IDirectoryDeletionService? directoryDeletionService = null)
    {
        _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem));
        _traversalService = traversalService ?? new DirectoryTraversalService(_directorySystem);
        _emptyDirectoryDetector = emptyDirectoryDetector ?? new EmptyDirectoryDetector(_directorySystem);
        _directoryDeletionService = directoryDeletionService ?? new DirectoryDeletionService(directoryDeleter ?? throw new ArgumentNullException(nameof(directoryDeleter)));
    }

    public static DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Default.Clean(root, options, cancellationToken);
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

        root = PathUtilities.NormalizeDirectoryPath(root);

        var empty = new List<string>();
        var deleted = new List<string>();
        var failures = new List<DirectoryCleanFailure>();
        var exclusions = new DirectoryExclusionEvaluator(root, options, failures);
        var traversalRequest = new DirectoryTraversalRequest(root, options, exclusions, failures);

        foreach (var directory in _traversalService.Enumerate(traversalRequest, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_directorySystem.Exists(directory))
            {
                continue;
            }

            if (!options.DeleteRootWhenEmpty && FileSystemPathComparer.PathComparer.Equals(directory, root))
            {
                continue;
            }

            if (!_emptyDirectoryDetector.IsEmpty(directory, failures))
            {
                continue;
            }

            empty.Add(directory);

            if (options.DryRun)
            {
                continue;
            }

            var mode = options.SendToRecycleBin
                ? DirectoryDeletionMode.RecycleBin
                : DirectoryDeletionMode.Permanent;

            if (_directoryDeletionService.TryDelete(directory, mode, failures))
            {
                deleted.Add(directory);
            }
        }

        return new DirectoryCleanResult(empty, deleted, failures);
    }
}
