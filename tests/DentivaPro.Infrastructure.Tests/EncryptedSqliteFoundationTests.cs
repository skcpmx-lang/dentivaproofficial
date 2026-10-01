using System.Security.Cryptography;
using System.Text;
using DentivaPro.Application.Abstractions;
using DentivaPro.Domain.Auditing;
using DentivaPro.Infrastructure.Persistence;
using DentivaPro.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DentivaPro.Infrastructure.Tests;

[TestClass]
public sealed class EncryptedSqliteFoundationTests
{
    private string _temporaryDirectory = string.Empty;

    [TestInitialize]
    public void SetUp() => _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"DentivaPro-db-tests-{Guid.NewGuid():N}");

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Initializer_AppliesMigrationAndPersistsSingletonMetadata()
    {
        var fixture = CreateFixture("clinic.db3", CreateKey(0x31));

        await fixture.Initializer.InitializeAsync();
        await fixture.Initializer.InitializeAsync();

        await using var context = fixture.ContextFactory.CreateDbContext();
        Assert.AreEqual(1, await context.ApplicationMetadata.CountAsync());
        var metadata = await context.ApplicationMetadata.SingleAsync();
        Assert.AreEqual(DatabaseMetadata.SingletonId, metadata.Id);
        Assert.AreEqual(DatabaseMetadata.InitialSchemaVersion, metadata.SchemaVersion);
        Assert.AreEqual("20261001000000_InitialFoundation", (await context.Database.GetAppliedMigrationsAsync()).Single());

        var header = new byte[16];
        await using (var database = new FileStream(fixture.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            await database.ReadExactlyAsync(header);
        }

        Assert.IsFalse(Encoding.ASCII.GetString(header).StartsWith("SQLite format 3", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task WrongDatabaseKey_CannotOpenTheEncryptedDatabase()
    {
        var correct = CreateFixture("clinic.db3", CreateKey(0x42));
        await correct.Initializer.InitializeAsync();

        var wrong = CreateFixture("clinic.db3", CreateKey(0x73));
        await Assert.ThrowsExceptionAsync<SqliteException>(() => wrong.Initializer.InitializeAsync());
    }

    [TestMethod]
    public async Task AuditWriter_PersistsOnlyStructuredIdentifiersAndUtcTime()
    {
        var fixture = CreateFixture("audit.db3", CreateKey(0x19));
        await fixture.Initializer.InitializeAsync();
        var correlationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 10, 1, 17, 15, 0, TimeSpan.FromHours(6));
        var auditEvent = AuditEvent.Create("SETUP.COMPLETE", timestamp, correlationId, actorId, "SETUP", entityId);

        await new EfAuditWriter(fixture.ContextFactory).WriteAsync(auditEvent);

        await using var context = fixture.ContextFactory.CreateDbContext();
        var saved = await context.AuditEvents.SingleAsync();
        Assert.AreEqual(auditEvent.Id, saved.Id);
        Assert.AreEqual(timestamp.ToUniversalTime().ToUnixTimeMilliseconds(), saved.OccurredAtUtcUnixMilliseconds);
        Assert.AreEqual(correlationId, saved.CorrelationId);
        Assert.AreEqual(actorId, saved.ActorId);
        Assert.AreEqual(entityId, saved.EntityId);
        Assert.AreEqual("SETUP.COMPLETE", saved.ActionCode);
    }

    [TestMethod]
    public async Task NativeEncryptedSqlite_RoundTripsBengaliUnicodeText()
    {
        var fixture = CreateFixture("unicode.db3", CreateKey(0x27));
        await fixture.Initializer.InitializeAsync();
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE UnicodeProbe (Value TEXT NOT NULL);";
            await create.ExecuteNonQueryAsync();
        }

        const string BengaliText = "দাঁতের চিকিৎসা — বাংলা Unicode";
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO UnicodeProbe (Value) VALUES ($value);";
            insert.Parameters.AddWithValue("$value", BengaliText);
            await insert.ExecuteNonQueryAsync();
        }

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT Value FROM UnicodeProbe LIMIT 1;";
        var loaded = (string?)await select.ExecuteScalarAsync();
        Assert.AreEqual(BengaliText, loaded);
    }

    private Fixture CreateFixture(string fileName, byte[] key)
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var databasePath = Path.Combine(_temporaryDirectory, fileName);
        var connectionFactory = new SqliteConnectionFactory(databasePath, new FixedDatabaseKeyStore(key), 5);
        var connectionString = connectionFactory.CreateConnectionString();
        var options = new DbContextOptionsBuilder<DentivaDbContext>()
            .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(typeof(DentivaDbContext).Assembly.GetName().Name))
            .AddInterceptors(new SqliteConnectionPolicyInterceptor(5))
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        var initializer = new SqliteDatabaseInitializer(contextFactory, databasePath);
        CryptographicOperations.ZeroMemory(key);
        return new Fixture(databasePath, connectionString, contextFactory, initializer);
    }

    private static byte[] CreateKey(byte value) => Enumerable.Repeat(value, FileDatabaseKeyStore.KeySizeBytes).ToArray();

    private sealed record Fixture(
        string DatabasePath,
        string ConnectionString,
        TestDbContextFactory ContextFactory,
        SqliteDatabaseInitializer Initializer);

    private sealed class FixedDatabaseKeyStore(byte[] key) : IDatabaseKeyStore
    {
        public byte[] GetOrCreateDatabaseKey() => key.ToArray();
    }

    private sealed class TestDbContextFactory(DbContextOptions<DentivaDbContext> options) : IDbContextFactory<DentivaDbContext>
    {
        public DentivaDbContext CreateDbContext() => new(options);

        public Task<DentivaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }
}
