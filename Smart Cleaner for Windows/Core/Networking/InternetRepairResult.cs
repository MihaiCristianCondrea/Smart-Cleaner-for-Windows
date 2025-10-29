using System;
using System.Collections.Generic;
using System.Linq;

namespace Smart_Cleaner_for_Windows.Core.Networking;

public sealed class InternetRepairResult
{
    public InternetRepairResult(IReadOnlyList<InternetRepairStepResult> steps)
    {
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public IReadOnlyList<InternetRepairStepResult> Steps { get; }

    public int SuccessCount => Steps.Count(static step => step.IsSuccess);

    public int FailureCount => Steps.Count(static step => !step.IsSuccess);
}

public sealed class InternetRepairStepResult
{
    public InternetRepairStepResult(
        InternetRepairAction action,
        bool isSuccess,
        int exitCode,
        string? output,
        string? error,
        string? message)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        IsSuccess = isSuccess;
        ExitCode = exitCode;
        Output = output;
        Error = error;
        Message = message;
    }

    public InternetRepairAction Action { get; }

    public bool IsSuccess { get; }

    public int ExitCode { get; }

    public string? Output { get; }

    public string? Error { get; }

    public string? Message { get; }
}
