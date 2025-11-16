using System;

namespace SmartCleanerForWindows.Core.Networking;

public sealed class InternetRepairAction(
    string id,
    string displayName,
    string description,
    string command,
    string? arguments,
    bool requiresElevation,
    string? successMessage = null)
{
    public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));

    public string DisplayName { get; } = displayName ?? throw new ArgumentNullException(nameof(displayName));

    public string Description { get; } = description ?? throw new ArgumentNullException(nameof(description));

    public string Command { get; } = command ?? throw new ArgumentNullException(nameof(command));

    public string Arguments { get; } = arguments ?? string.Empty;

    public bool RequiresElevation { get; } = requiresElevation;

    public string? SuccessMessage { get; } = successMessage;
}
