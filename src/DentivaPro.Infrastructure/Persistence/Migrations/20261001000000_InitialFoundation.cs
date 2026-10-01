using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentivaPro.Infrastructure.Persistence.Migrations;

public partial class InitialFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApplicationMetadata",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtcUnixMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApplicationMetadata", value => value.Id);
                table.CheckConstraint("CK_ApplicationMetadata_SingletonId", "\"Id\" = 1");
                table.CheckConstraint("CK_ApplicationMetadata_SchemaVersion", "\"SchemaVersion\" >= 1");
            });

        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OccurredAtUtcUnixMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                ActorId = table.Column<Guid>(type: "TEXT", nullable: true),
                ActionCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                EntityId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", value => value.Id);
                table.CheckConstraint(
                    "CK_AuditEvents_EntityReferencePair",
                    "(\"EntityType\" IS NULL AND \"EntityId\" IS NULL) OR (\"EntityType\" IS NOT NULL AND \"EntityId\" IS NOT NULL)");
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_ActorId",
            table: "AuditEvents",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_OccurredAtUtcUnixMilliseconds",
            table: "AuditEvents",
            column: "OccurredAtUtcUnixMilliseconds");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApplicationMetadata");
        migrationBuilder.DropTable(name: "AuditEvents");
    }
}
