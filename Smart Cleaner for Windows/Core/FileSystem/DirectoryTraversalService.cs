using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class DirectoryTraversalService(IDirectorySystem directorySystem) : IDirectoryTraversalService
{
    private readonly IDirectorySystem _directorySystem = directorySystem ?? throw new ArgumentNullException(nameof(directorySystem));

    public IEnumerable<string> Enumerate(DirectoryTraversalRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var pending = new Stack<(string Path, int Depth)>();
        var ordered = new Stack<string>();
        pending.Push((request.Root, 0));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (current, depth) = pending.Pop();
            ordered.Push(current);

            if (request.Options.MaxDepth is { } maxDepth && depth >= maxDepth)
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
                request.Failures.Add(new DirectoryCleanFailure(current, ex));
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalized = PathUtilities.NormalizeDirectoryPath(child);

                if (request.Exclusions.ShouldExclude(normalized))
                {
                    continue;
                }

                if (request.Options.SkipReparsePoints && IsReparsePoint(normalized, request.Failures))
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
}
