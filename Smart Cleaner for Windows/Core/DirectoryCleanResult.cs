using System;
using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core;

/// <summary>
/// Represents the outcome of a directory cleaning operation.
/// </summary>
/// <param name="EmptyDirectories">All directories that were identified as empty at scan time.</param>
/// <param name="DeletedDirectories">Directories that were successfully deleted.</param>
/// <param name="Failures">Errors that occurred while enumerating or deleting directories.</param>
public sealed record DirectoryCleanResult(
    IReadOnlyList<string> EmptyDirectories,
    IReadOnlyList<string> DeletedDirectories,
    IReadOnlyList<DirectoryCleanFailure> Failures)
{
    /// <summary>
    /// Gets a value indicating whether any directories were deleted.
    /// </summary>
    public int DeletedCount => DeletedDirectories.Count;

    /// <summary>
    /// Gets a value indicating the number of empty directories that were discovered.
    /// </summary>
    public int EmptyFound => EmptyDirectories.Count;

    /// <summary>
    /// Gets a value indicating whether any error occurred while processing directories.
    /// </summary>
    public bool HasFailures => Failures.Count > 0;
}

/// <summary>
/// Describes a failure that happened when interacting with a directory.
/// </summary>
/// <param name="Path">The path that triggered the error.</param>
/// <param name="Exception">The captured exception.</param>
public sealed record DirectoryCleanFailure(string Path, Exception Exception); // FIXME: Positional property 'Smart_Cleaner_for_Windows.Core.DirectoryCleanFailure.Path' is never accessed (except in implicit Equals/ToString implementations) && Positional property 'Smart_Cleaner_for_Windows.Core.DirectoryCleanFailure.Exception' is never accessed (except in implicit Equals/ToString implementations)
