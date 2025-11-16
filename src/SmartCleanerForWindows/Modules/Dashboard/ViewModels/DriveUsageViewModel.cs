namespace SmartCleanerForWindows.Modules.Dashboard.ViewModels;

public sealed class DriveUsageViewModel(string name, string details, double usedPercentage, string usageSummary)
{
    public string Name { get; } = name;

    public string Details { get; } = details;

    public double UsedPercentage { get; } = usedPercentage;

    public string UsageSummary { get; } = usageSummary;
}
