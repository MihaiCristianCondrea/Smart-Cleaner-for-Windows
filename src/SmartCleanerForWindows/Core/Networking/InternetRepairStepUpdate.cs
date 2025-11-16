using System;

namespace SmartCleanerForWindows.Core.Networking;

public enum InternetRepairStepState
{
    Starting,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class InternetRepairStepUpdate(
    InternetRepairAction action,
    InternetRepairStepState state,
    string? message)
{
    public InternetRepairAction Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

    public InternetRepairStepState State { get; } = state;

    public string? Message { get; } = message;
}
