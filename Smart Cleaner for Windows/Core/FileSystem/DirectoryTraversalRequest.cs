using System;
using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

internal sealed class DirectoryTraversalRequest
{
    public DirectoryTraversalRequest(
        string root,
        DirectoryCleanOptions options,
        IDirectoryExclusionEvaluator exclusions,
        ICollection<DirectoryCleanFailure> failures)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Exclusions = exclusions ?? throw new ArgumentNullException(nameof(exclusions));
        Failures = failures ?? throw new ArgumentNullException(nameof(failures));
    }

    public string Root { get; }

    public DirectoryCleanOptions Options { get; }

    public IDirectoryExclusionEvaluator Exclusions { get; }

    public ICollection<DirectoryCleanFailure> Failures { get; }
}
