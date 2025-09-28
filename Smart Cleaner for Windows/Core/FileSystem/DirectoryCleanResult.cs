using System;
using System.Collections.Generic;

namespace Smart_Cleaner_for_Windows.Core.FileSystem;

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
    public DirectoryCleanResult()
        : this(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<DirectoryCleanFailure>())
    {
    }

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
public sealed record DirectoryCleanFailure
{
    public DirectoryCleanFailure()
        : this(string.Empty, new InvalidOperationException("Uninitialized failure"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryCleanFailure"/> class.
    /// </summary>
    /// <param name="path">The path that triggered the error.</param>
    /// <param name="exception">The captured exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is <see langword="null"/>, empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public DirectoryCleanFailure(string path, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The path associated with the failure must be provided.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(exception);

        Path = path;
        Exception = exception;
    }

    /// <summary>
    /// Gets the path that triggered the error.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the captured exception.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Returns a string representation of the failure.
    /// </summary>
    /// <returns>A human-readable description that includes the failing path.</returns>
    public override string ToString()
    {
        return $"{Path}: {Exception.Message}";
    }
}
