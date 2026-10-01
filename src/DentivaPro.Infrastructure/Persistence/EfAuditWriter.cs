using DentivaPro.Application.Abstractions;
using DentivaPro.Domain.Auditing;

namespace DentivaPro.Infrastructure.Persistence;

public sealed class EfAuditWriter(IDbContextFactory<DentivaDbContext> contextFactory) : IAuditWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
