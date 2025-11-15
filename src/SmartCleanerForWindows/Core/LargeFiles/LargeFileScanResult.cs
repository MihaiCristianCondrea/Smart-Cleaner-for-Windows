using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileScanResult
{
    public LargeFileScanResult()
        : this([], [])
    {
    }

    public LargeFileScanResult(IEnumerable<LargeFileEntry> files, IEnumerable<LargeFileScanFailure> failures) // FIXME: Cannot resolve symbol 'LargeFileEntry'
    {
        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        if (failures is null)
        {
            throw new ArgumentNullException(nameof(failures));
        }

        var fileList = files.ToList();
        var failureList = failures.ToList();

        Files = new ReadOnlyCollection<LargeFileEntry>(fileList); // FIXME: Cannot resolve symbol 'LargeFileEntry'
        Failures = new ReadOnlyCollection<LargeFileScanFailure>(failureList);
        fileList.Aggregate(0L, (current, entry) => current + Math.Max(0L, entry.Size)); // FIXME: <html>Ambiguous invocation.<br/>Candidates are:<br/>Max(decimal, decimal) : decimal<br/>Max(double, double) : double<br/>Max(float, float) : float<br/>Max(long, long) : long<br/>Max(ulong, ulong) : ulong<br/>all from class Math
    }

    public IReadOnlyList<LargeFileEntry> Files { get; } // FIXME: Cannot resolve symbol 'LargeFileEntry'

    public IReadOnlyList<LargeFileScanFailure> Failures { get; }

    public int FileCount => Files.Count;

    public bool HasFailures => Failures.Count > 0;
}
