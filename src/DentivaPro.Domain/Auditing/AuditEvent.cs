namespace DentivaPro.Domain.Auditing;

/// <summary>
/// A minimal, structured audit fact. It deliberately has no free-form clinical or contact-data fields.
/// </summary>
public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public Guid Id { get; private set; }

    public long OccurredAtUtcUnixMilliseconds { get; private set; }

    public Guid CorrelationId { get; private set; }

    public Guid? ActorId { get; private set; }

    public string ActionCode { get; private set; } = string.Empty;

    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public DateTimeOffset OccurredAtUtc => DateTimeOffset.FromUnixTimeMilliseconds(OccurredAtUtcUnixMilliseconds);

    public static AuditEvent Create(
        string actionCode,
        DateTimeOffset occurredAtUtc,
        Guid correlationId,
        Guid? actorId = null,
        string? entityType = null,
        Guid? entityId = null)
    {
        if (!IsCode(actionCode, 80))
        {
            throw new ArgumentException("Action codes must be short, uppercase identifiers.", nameof(actionCode));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty correlation identifier is required.", nameof(correlationId));
        }

        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor identifiers cannot be empty.", nameof(actorId));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity identifiers cannot be empty.", nameof(entityId));
        }

        if ((entityType is null) != (entityId is null))
        {
            throw new ArgumentException("Entity type and entity identifier must be supplied together.", nameof(entityType));
        }

        if (entityType is not null && !IsCode(entityType, 64))
        {
            throw new ArgumentException("Entity types must be short, uppercase identifiers.", nameof(entityType));
        }

        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtcUnixMilliseconds = occurredAtUtc.ToUniversalTime().ToUnixTimeMilliseconds(),
            CorrelationId = correlationId,
            ActorId = actorId,
            ActionCode = actionCode,
            EntityType = entityType,
            EntityId = entityId
        };
    }

    private static bool IsCode(string value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!(character is >= 'A' and <= 'Z') &&
                !(character is >= '0' and <= '9') &&
                character is not '.' and not '_' and not '-')
            {
                return false;
            }
        }

        return true;
    }
}
