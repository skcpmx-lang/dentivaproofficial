using System.Security.Cryptography;
using DentivaPro.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace DentivaPro.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private static readonly Lazy<bool> NativeProviderInitialization = new(() =>
    {
        SQLitePCL.Batteries_V2.Init();
        return true;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly string _databasePath;
    private readonly IDatabaseKeyStore _keyStore;
    private readonly int _defaultTimeoutSeconds;

    public SqliteConnectionFactory(string databasePath, IDatabaseKeyStore keyStore, int defaultTimeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(keyStore);
        if (defaultTimeoutSeconds is < 1 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultTimeoutSeconds));
        }

        _databasePath = Path.GetFullPath(databasePath);
        _keyStore = keyStore;
        _defaultTimeoutSeconds = defaultTimeoutSeconds;
    }

    /// <remarks>
    /// The returned string contains the encryption passphrase. Never log or expose it to UI/configuration.
    /// The database key itself is random; it is not the user's login password.
    /// </remarks>
    public string CreateConnectionString()
    {
        _ = NativeProviderInitialization.Value;
        var directory = Path.GetDirectoryName(_databasePath)
            ?? throw new InvalidOperationException("The database path has no parent directory.");
        Directory.CreateDirectory(directory);

        var key = _keyStore.GetOrCreateDatabaseKey();
        try
        {
            if (key.Length != FileDatabaseKeyStore.KeySizeBytes)
            {
                throw new CryptographicException("The database key has an invalid size.");
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
                DefaultTimeout = _defaultTimeoutSeconds,
                ForeignKeys = true,
                Password = Convert.ToBase64String(key)
            };

            return builder.ToString();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
