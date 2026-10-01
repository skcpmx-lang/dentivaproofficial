using System.Data.Common;
using DentivaPro.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DentivaPro.Infrastructure.Persistence;

/// <summary>Applies versioned migrations and validates the newly opened encrypted database.</summary>
public sealed class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<DentivaDbContext> _contextFactory;
    private readonly string _databasePath;

    public SqliteDatabaseInitializer(IDbContextFactory<DentivaDbContext> contextFactory, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _contextFactory = contextFactory;
        _databasePath = Path.GetFullPath(databasePath);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath)
            ?? throw new InvalidOperationException("The database path has no parent directory.");
        Directory.CreateDirectory(directory);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnableWriteAheadLoggingAsync(context.Database.GetDbConnection(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await ValidateIntegrityAsync(context, cancellationToken).ConfigureAwait(false);
        await EnsureMetadataAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnableWriteAheadLoggingAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture), "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The database did not accept the required write-ahead logging mode.");
        }
    }

    private static async Task ValidateIntegrityAsync(DentivaDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The encrypted database failed its integrity check.");
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static async Task EnsureMetadataAsync(DentivaDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await context.ApplicationMetadata.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            context.ApplicationMetadata.Add(DatabaseMetadata.Create(DateTimeOffset.UtcNow));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (metadata.Id != DatabaseMetadata.SingletonId || metadata.SchemaVersion != DatabaseMetadata.InitialSchemaVersion)
        {
            throw new InvalidDataException("The application database metadata is unsupported.");
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
