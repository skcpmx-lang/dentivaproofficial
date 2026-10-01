namespace DentivaPro.Infrastructure.Persistence;

public sealed class DatabaseMetadata
{
    public const int SingletonId = 1;
    public const int InitialSchemaVersion = 1;

    private DatabaseMetadata()
    {
    }

    public int Id { get; private set; }

    public int SchemaVersion { get; private set; }

    public long CreatedAtUtcUnixMilliseconds { get; private set; }

    public DateTimeOffset CreatedAtUtc => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAtUtcUnixMilliseconds);

    public static DatabaseMetadata Create(DateTimeOffset createdAtUtc) => new()
    {
        Id = SingletonId,
        SchemaVersion = InitialSchemaVersion,
        CreatedAtUtcUnixMilliseconds = createdAtUtc.ToUniversalTime().ToUnixTimeMilliseconds()
    };
}
