namespace Smart_Cleaner_for_Windows.Core.Networking;

public static class InternetRepairServiceFactory
{
    public static IInternetRepairService CreateDefault() => new InternetRepairService();
}
