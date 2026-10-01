using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DentivaPro.Infrastructure.Persistence;

/// <summary>Applies connection-local SQLite durability and foreign-key policy on every opened connection.</summary>
public sealed class SqliteConnectionPolicyInterceptor : DbConnectionInterceptor
{
    private readonly int _busyTimeoutMilliseconds;

    public SqliteConnectionPolicyInterceptor(int busyTimeoutSeconds)
    {
        if (busyTimeoutSeconds is < 1 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(busyTimeoutSeconds));
        }

        _busyTimeoutMilliseconds = checked(busyTimeoutSeconds * 1000);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = GetPolicySql();
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = GetPolicySql();
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private string GetPolicySql() =>
        $"PRAGMA busy_timeout={_busyTimeoutMilliseconds}; PRAGMA foreign_keys=ON; PRAGMA synchronous=FULL; PRAGMA temp_store=MEMORY;";
}
