using DentivaPro.Domain.Auditing;

namespace DentivaPro.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
