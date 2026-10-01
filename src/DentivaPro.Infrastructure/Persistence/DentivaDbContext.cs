using DentivaPro.Domain.Auditing;

namespace DentivaPro.Infrastructure.Persistence;

public sealed class DentivaDbContext(DbContextOptions<DentivaDbContext> options) : DbContext(options)
{
    public DbSet<DatabaseMetadata> ApplicationMetadata => Set<DatabaseMetadata>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatabaseMetadata>(entity =>
        {
            entity.ToTable("ApplicationMetadata", table =>
            {
                table.HasCheckConstraint("CK_ApplicationMetadata_SingletonId", "\"Id\" = 1");
                table.HasCheckConstraint("CK_ApplicationMetadata_SchemaVersion", "\"SchemaVersion\" >= 1");
            });
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).ValueGeneratedNever();
            entity.Property(value => value.SchemaVersion).IsRequired();
            entity.Property(value => value.CreatedAtUtcUnixMilliseconds).IsRequired();
            entity.Ignore(value => value.CreatedAtUtc);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents", table =>
                table.HasCheckConstraint(
                    "CK_AuditEvents_EntityReferencePair",
                    "(\"EntityType\" IS NULL AND \"EntityId\" IS NULL) OR (\"EntityType\" IS NOT NULL AND \"EntityId\" IS NOT NULL)"));
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).ValueGeneratedNever();
            entity.Property(value => value.ActionCode).HasMaxLength(80).IsRequired();
            entity.Property(value => value.ActorId);
            entity.Property(value => value.EntityType).HasMaxLength(64);
            entity.Property(value => value.EntityId);
            entity.Property(value => value.CorrelationId).IsRequired();
            entity.Property(value => value.OccurredAtUtcUnixMilliseconds).IsRequired();
            entity.Ignore(value => value.OccurredAtUtc);
            entity.HasIndex(value => value.OccurredAtUtcUnixMilliseconds);
            entity.HasIndex(value => value.ActorId);
        });
    }
}
