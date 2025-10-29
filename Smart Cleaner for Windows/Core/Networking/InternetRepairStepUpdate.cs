using System;

namespace Smart_Cleaner_for_Windows.Core.Networking;

public enum InternetRepairStepState
{
    Starting,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class InternetRepairStepUpdate
{
    public InternetRepairStepUpdate(InternetRepairAction action, InternetRepairStepState state, string? message)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        State = state;
        Message = message;
    }

    public InternetRepairAction Action { get; }

    public InternetRepairStepState State { get; }

    public string? Message { get; }
}
