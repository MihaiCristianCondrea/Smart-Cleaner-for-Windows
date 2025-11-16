namespace SmartCleanerForWindows.Core.Networking;

public static class InternetRepairServiceFactory
{
    public static IInternetRepairService CreateDefault() => new InternetRepairService();
}
