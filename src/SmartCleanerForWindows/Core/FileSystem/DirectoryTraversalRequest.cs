using System;
using System.Collections.Generic;

namespace SmartCleanerForWindows.Core.FileSystem;

public sealed class DirectoryTraversalRequest(
    string root,
    DirectoryCleanOptions options,
    IDirectoryExclusionEvaluator exclusions,
    ICollection<DirectoryCleanFailure> failures)
{
    public string Root { get; } = root ?? throw new ArgumentNullException(nameof(root));

    public DirectoryCleanOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    public IDirectoryExclusionEvaluator Exclusions { get; } = exclusions ?? throw new ArgumentNullException(nameof(exclusions));

    public ICollection<DirectoryCleanFailure> Failures { get; } = failures ?? throw new ArgumentNullException(nameof(failures));
}
