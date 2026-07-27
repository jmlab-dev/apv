namespace AdrenalinProfileViewer.Services;

/// <summary>
/// Centralizes every path the application itself is allowed to write to.
/// All persistent data lives in a data folder beside the running executable.
/// </summary>
public static class PortablePaths
{
    public static string ApplicationDirectory { get; } =
        Path.GetFullPath(AppContext.BaseDirectory);

    public static string DataDirectory { get; } =
        Path.Combine(ApplicationDirectory, "data");

    public static string SettingsDirectory { get; } =
        Path.Combine(DataDirectory, "settings");

    public static string ProfileMetadataDirectory { get; } =
        Path.Combine(DataDirectory, "profile-metadata");

    public static string LogsDirectory { get; } =
        Path.Combine(DataDirectory, "logs");

    public static string ExportsDirectory { get; } =
        Path.Combine(DataDirectory, "exports");

    public static string ProfilesDirectory { get; } =
        Path.Combine(ApplicationDirectory, "profiles");

    public static string SessionFilePath { get; } =
        Path.Combine(SettingsDirectory, "session.json");

    public static string CrashLogPath { get; } =
        Path.Combine(LogsDirectory, "crash.log");

    public static string SessionLogPath { get; } =
        Path.Combine(LogsDirectory, "session.log");

    public static void Initialize()
    {
        MigrateLegacyPortableData();
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(SettingsDirectory);
        Directory.CreateDirectory(ProfileMetadataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(ProfilesDirectory);
    }

    private static void MigrateLegacyPortableData()
    {
        var legacy = Path.Combine(ApplicationDirectory, "portable-data");
        if (Directory.Exists(DataDirectory) || !Directory.Exists(legacy))
        {
            return;
        }

        try
        {
            Directory.Move(legacy, DataDirectory);
        }
        catch
        {
            // Migration is optional. If it cannot be moved, a fresh data folder is used.
        }
    }
}
