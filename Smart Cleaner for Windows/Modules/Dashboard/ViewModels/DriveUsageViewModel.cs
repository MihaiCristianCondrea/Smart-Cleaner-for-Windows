namespace Smart_Cleaner_for_Windows.Modules.Dashboard.ViewModels;

public sealed class DriveUsageViewModel
{
    public DriveUsageViewModel(string name, string details, double usedPercentage, string usageSummary)
    {
        Name = name;
        Details = details;
        UsedPercentage = usedPercentage;
        UsageSummary = usageSummary;
    }

    public string Name { get; }

    public string Details { get; }

    public double UsedPercentage { get; }

    public string UsageSummary { get; }
}
