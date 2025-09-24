using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileScanResult
{
    public LargeFileScanResult(IEnumerable<LargeFileEntry> files, IEnumerable<LargeFileScanFailure> failures)
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

        Files = new ReadOnlyCollection<LargeFileEntry>(fileList);
        Failures = new ReadOnlyCollection<LargeFileScanFailure>(failureList);
        TotalSize = fileList.Aggregate(0L, (current, entry) => current + Math.Max(0L, entry.Size));
    }

    public IReadOnlyList<LargeFileEntry> Files { get; }

    public IReadOnlyList<LargeFileScanFailure> Failures { get; }

    public long TotalSize { get; }

    public int FileCount => Files.Count;

    public bool HasFailures => Failures.Count > 0;
}
