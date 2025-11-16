using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCleanerForWindows.Core.Networking;

public sealed class InternetRepairResult(IReadOnlyList<InternetRepairStepResult> steps)
{
    private IReadOnlyList<InternetRepairStepResult> Steps { get; } = steps ?? throw new ArgumentNullException(nameof(steps));

    public int SuccessCount => Steps.Count(static step => step.IsSuccess);

    public int FailureCount => Steps.Count(static step => !step.IsSuccess);
}

public sealed class InternetRepairStepResult(
    InternetRepairAction action,
    bool isSuccess,
    int exitCode,
    string? output,
    string? error,
    string? message)
{
    public InternetRepairAction Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

    public bool IsSuccess { get; } = isSuccess;

    public int ExitCode { get; } = exitCode;

    public string? Output { get; } = output;

    public string? Error { get; } = error;

    public string? Message { get; } = message;
}
