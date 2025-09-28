using System;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileScanFailure(string path, Exception exception)
{
    public string Path { get; } = path;

    public Exception Exception { get; } = exception;
}
