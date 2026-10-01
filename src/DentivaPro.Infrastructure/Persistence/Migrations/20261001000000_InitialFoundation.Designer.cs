using DentivaPro.Domain.Auditing;
using DentivaPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentivaPro.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentivaDbContext))]
[Migration("20261001000000_InitialFoundation")]
partial class InitialFoundation
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.12")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

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
