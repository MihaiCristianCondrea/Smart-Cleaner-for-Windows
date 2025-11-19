namespace SmartCleanerForWindows.Settings;

public static class ToolSettingsPaths
{
    private const string SettingsDirectoryName = "SettingsDefinitions";
    private const string UserSettingsDirectoryName = "Settings";
    private const string AppFolderName = "SmartCleanerForWindows";

    public static string GetDefinitionRoot(string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, SettingsDirectoryName);
    }

    public static string GetUserSettingsRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName, UserSettingsDirectoryName);
    }
}
