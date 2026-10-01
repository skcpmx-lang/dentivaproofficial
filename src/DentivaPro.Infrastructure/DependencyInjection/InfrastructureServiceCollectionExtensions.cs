using System.Globalization;
using System.Runtime.Versioning;
using DentivaPro.Application.Abstractions;
using DentivaPro.Application.Security;
using DentivaPro.Infrastructure.Persistence;
using DentivaPro.Infrastructure.Security;
using DentivaPro.Infrastructure.Storage;

namespace DentivaPro.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddDentivaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        ApplicationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(paths);

        var timeoutSeconds = ReadBoundedInteger(configuration["Database:BusyTimeoutSeconds"], 5, 1, 30, "Database:BusyTimeoutSeconds");

        services.AddSingleton(paths);
        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        services.AddSingleton<IDatabaseKeyStore>(provider =>
            new FileDatabaseKeyStore(paths.DatabaseKeyPath, paths.DatabasePath, provider.GetRequiredService<ISecretProtector>()));
        services.AddSingleton(provider => new SqliteConnectionFactory(
            paths.DatabasePath,
            provider.GetRequiredService<IDatabaseKeyStore>(),
            timeoutSeconds));
        services.AddSingleton(provider => new SqliteConnectionPolicyInterceptor(timeoutSeconds));
        services.AddDbContextFactory<DentivaDbContext>((provider, options) =>
        {
            var connectionFactory = provider.GetRequiredService<SqliteConnectionFactory>();
            options.UseSqlite(
                connectionFactory.CreateConnectionString(),
                sqlite => sqlite.MigrationsAssembly(typeof(DentivaDbContext).Assembly.GetName().Name));
            options.AddInterceptors(provider.GetRequiredService<SqliteConnectionPolicyInterceptor>());
        });
        services.AddSingleton<IDatabaseInitializer>(provider => new SqliteDatabaseInitializer(
            provider.GetRequiredService<IDbContextFactory<DentivaDbContext>>(),
            paths.DatabasePath));
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<IAuditWriter, EfAuditWriter>();

        return services;
    }

    private static int ReadBoundedInteger(string? value, int defaultValue, int minimum, int maximum, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException($"Configuration value '{key}' must be between {minimum} and {maximum}.");
        }

        return parsed;
    }
}
