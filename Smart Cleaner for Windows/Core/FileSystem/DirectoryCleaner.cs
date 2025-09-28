using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Smart_Cleaner_for_Windows.Core.FileSystem;

/// <summary>
/// Provides helpers to identify and remove empty directories.
/// </summary>
public sealed class DirectoryCleaner : IDirectoryCleaner
{
    private readonly IDirectorySystem _directorySystem;
    private readonly IDirectoryTraversalService _traversalService;
    private readonly IEmptyDirectoryDetector _emptyDirectoryDetector;
    private readonly IDirectoryDeletionService _directoryDeletionService;

    public static IDirectoryCleaner Default { get; } = DirectoryCleanerFactory.CreateDefault();

    public DirectoryCleaner(
        IDirectorySystem directorySystem,
        IDirectoryTraversalService traversalService,
        IEmptyDirectoryDetector emptyDirectoryDetector,
        IDirectoryDeletionService directoryDeletionService)
    {
        _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem));
        _traversalService = traversalService ?? throw new ArgumentNullException(nameof(traversalService));
        _emptyDirectoryDetector = emptyDirectoryDetector ?? throw new ArgumentNullException(nameof(emptyDirectoryDetector));
        _directoryDeletionService = directoryDeletionService
            ?? throw new ArgumentNullException(nameof(directoryDeletionService));
    }

    public static DirectoryCleanResult Clean(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
        => Default.Clean(root, options, cancellationToken);

    DirectoryCleanResult IDirectoryCleaner.Clean(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
        => CleanInternal(root, options, cancellationToken);

    private Task<DirectoryCleanResult> CleanAsync(string root, DirectoryCleanOptions? options = null, CancellationToken cancellationToken = default)
        => Task.Run(() => CleanInternal(root, options, cancellationToken), cancellationToken);

    Task<DirectoryCleanResult> IDirectoryCleaner.CleanAsync(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
        => CleanAsync(root, options, cancellationToken);

    /// <summary>
    /// Scans the directory tree rooted at <paramref name="root"/> and optionally deletes empty directories.
    /// </summary>
    private DirectoryCleanResult CleanInternal(string root, DirectoryCleanOptions? options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentNullException(nameof(root));

        options ??= DirectoryCleanOptions.Default;

        if (!_directorySystem.Exists(root))
            throw new DirectoryNotFoundException($"The directory '{root}' does not exist.");

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
                continue;

            if (!options.DeleteRootWhenEmpty && FileSystemPathComparer.PathComparer.Equals(directory, root))
                continue;

            if (!_emptyDirectoryDetector.IsEmpty(directory, failures))
                continue;

            empty.Add(directory);

            if (options.DryRun)
                continue;

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