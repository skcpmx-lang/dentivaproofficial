using System.Globalization;
using DentivaPro.Infrastructure.DependencyInjection;
using DentivaPro.Infrastructure.Logging;
using DentivaPro.Infrastructure.Storage;

namespace DentivaPro.Desktop;

internal static class AppHostBuilder
{
    public static IHost Create(string[] args)
    {
        var paths = ApplicationPaths.ForCurrentUser();
        paths.EnsureDirectories();

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                configuration.AddEnvironmentVariables("DENTIVAPRO_");
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(ReadMinimumLevel(context.Configuration["Logging:MinimumLevel"]));
                logging.AddProvider(new PrivacyPreservingJsonFileLoggerProvider(
                    paths.LogDirectory,
                    ReadBoundedLong(context.Configuration["Logging:MaximumFileBytes"], 5 * 1024 * 1024, 4096, 100 * 1024 * 1024),
                    ReadBoundedInteger(context.Configuration["Logging:RetentionDays"], 14, 1, 90)));
            })
            .ConfigureServices((context, services) =>
            {
                SetDefaultCulture(context.Configuration["Application:Culture"] ?? "bn-BD");
                services.AddDentivaInfrastructure(context.Configuration, paths);
                services.AddTransient<MainWindow>();
            })
            .Build();
    }

    private static void SetDefaultCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static LogLevel ReadMinimumLevel(string? value) =>
        Enum.TryParse<LogLevel>(value, ignoreCase: true, out var level) && Enum.IsDefined(level)
            ? level
            : LogLevel.Information;

    private static int ReadBoundedInteger(string? value, int defaultValue, int minimum, int maximum)
    {
        var parsed = ReadBoundedLong(value, defaultValue, minimum, maximum);
        return checked((int)parsed);
    }

    private static long ReadBoundedLong(string? value, long defaultValue, long minimum, long maximum)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException("An application configuration value is outside its supported range.");
        }

        return parsed;
    }
}
