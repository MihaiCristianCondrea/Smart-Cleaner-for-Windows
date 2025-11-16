using System;

namespace SmartCleanerForWindows.Core.LargeFiles;

public sealed class LargeFileScanFailure(string path, Exception exception)
{
    public string Path { get; } = path;

    public Exception Exception { get; } = exception;
}
