using System;

namespace Smart_Cleaner_for_Windows.Core.Networking;

public sealed class InternetRepairAction
{
    public InternetRepairAction(
        string id,
        string displayName,
        string description,
        string command,
        string arguments,
        bool requiresElevation,
        string? successMessage = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Arguments = arguments ?? string.Empty;
        RequiresElevation = requiresElevation;
        SuccessMessage = successMessage;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string Command { get; }

    public string Arguments { get; }

    public bool RequiresElevation { get; }

    public string? SuccessMessage { get; }
}
