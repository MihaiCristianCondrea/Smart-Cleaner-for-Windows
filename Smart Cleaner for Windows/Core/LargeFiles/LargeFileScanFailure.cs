using System;

namespace Smart_Cleaner_for_Windows.Core.LargeFiles;

public sealed class LargeFileScanFailure
{
    public LargeFileScanFailure(string path, Exception exception)
    {
        Path = path;
        Exception = exception;
    }

    public string Path { get; }

    public Exception Exception { get; }
}
