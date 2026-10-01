namespace DentivaPro.Infrastructure.Storage;

/// <summary>Per-Windows-profile storage locations. No clinic data is placed beside the executable.</summary>
public sealed class ApplicationPaths
{
    private ApplicationPaths(string applicationDirectory)
    {
        ApplicationDirectory = Path.GetFullPath(applicationDirectory);
        DatabasePath = Path.Combine(ApplicationDirectory, "dentiva-pro.db3");
        DatabaseKeyPath = Path.Combine(ApplicationDirectory, "Key", "database-key.dpapi");
        LogDirectory = Path.Combine(ApplicationDirectory, "Logs");
        ConfigurationDirectory = Path.Combine(ApplicationDirectory, "Config");
    }

    public string ApplicationDirectory { get; }

    public string DatabasePath { get; }

    public string DatabaseKeyPath { get; }

    public string LogDirectory { get; }

    public string ConfigurationDirectory { get; }

    public static ApplicationPaths ForCurrentUser()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The current user's local application-data directory is unavailable.");
        }

        return ForRoot(Path.Combine(localApplicationData, "DentivaPro"));
    }

    public static ApplicationPaths ForRoot(string applicationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        return new ApplicationPaths(applicationDirectory);
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(ApplicationDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabaseKeyPath)!);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(ConfigurationDirectory);
    }
}
