using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioCPR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentDraftFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftPayload",
                table: "equipment",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAtUtc",
                table: "equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationRevision",
                table: "equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationState",
                table: "equipment",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Definitive"); // Existing equipment remains definitive, even without photographs.

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrationUpdatedAtUtc",
                table: "equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRegistrationPhoto",
                table: "equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false); // Explicit legacy exemption, never inferred from missing evidence.

            migrationBuilder.Sql("ALTER TABLE equipment ALTER COLUMN \"RegistrationState\" SET DEFAULT 'Draft';");
            migrationBuilder.Sql("ALTER TABLE equipment ALTER COLUMN \"RequiresRegistrationPhoto\" SET DEFAULT TRUE;");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_RegistrationState_CodeNormalized",
                table: "equipment",
                columns: new[] { "RegistrationState", "CodeNormalized" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_equipment_draft_size",
                table: "equipment",
                sql: "\"DraftPayload\" IS NULL OR length(\"DraftPayload\") <= 65536");

            migrationBuilder.AddCheckConstraint(
                name: "CK_equipment_registration",
                table: "equipment",
                sql: "\"RegistrationState\" IN ('Draft','Definitive') AND \"RegistrationRevision\" >= 0 AND (\"RegistrationState\" <> 'Definitive' OR (\"DraftPayload\" IS NULL AND (NOT \"RequiresRegistrationPhoto\" OR \"FinalizedAtUtc\" IS NOT NULL)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM equipment WHERE "RequiresRegistrationPhoto" OR "RegistrationState" = 'Draft') THEN
                        RAISE EXCEPTION 'No se puede retirar el estado de registro mientras existan equipos nuevos o borradores.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropIndex(
                name: "IX_equipment_RegistrationState_CodeNormalized",
                table: "equipment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_equipment_draft_size",
                table: "equipment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_equipment_registration",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "DraftPayload",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "FinalizedAtUtc",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "RegistrationRevision",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "RegistrationState",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "RegistrationUpdatedAtUtc",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "RequiresRegistrationPhoto",
                table: "equipment");
        }
    }
}
