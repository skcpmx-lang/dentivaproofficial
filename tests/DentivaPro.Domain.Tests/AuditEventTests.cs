using DentivaPro.Domain.Auditing;

namespace DentivaPro.Domain.Tests;

[TestClass]
public sealed class AuditEventTests
{
    [TestMethod]
    public void Create_NormalizesTimestampToUtcAndUsesIdentifiersOnly()
    {
        var localTime = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.FromHours(6));
        var correlationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var auditEvent = AuditEvent.Create("PATIENT.CREATED", localTime, correlationId, actorId, "PATIENT", entityId);

        Assert.AreEqual(localTime.ToUniversalTime().ToUnixTimeMilliseconds(), auditEvent.OccurredAtUtcUnixMilliseconds);
        Assert.AreEqual(correlationId, auditEvent.CorrelationId);
        Assert.AreEqual(actorId, auditEvent.ActorId);
        Assert.AreEqual(entityId, auditEvent.EntityId);
        Assert.AreEqual("PATIENT.CREATED", auditEvent.ActionCode);
    }

    [TestMethod]
    public void Create_RejectsFreeFormActionOrEntityText()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            AuditEvent.Create("Patient Alice created", DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.ThrowsExactly<ArgumentException>(() =>
            AuditEvent.Create("PATIENT.CREATED", DateTimeOffset.UtcNow, Guid.NewGuid(), entityType: "Patient name"));
    }

    [TestMethod]
    public void Create_RequiresCompleteEntityReference()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            AuditEvent.Create("PATIENT.OPENED", DateTimeOffset.UtcNow, Guid.NewGuid(), entityType: "PATIENT"));
    }
}
