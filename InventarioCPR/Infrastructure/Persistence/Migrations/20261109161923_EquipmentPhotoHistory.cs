using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioCPR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentPhotoHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_photo_evidence_EquipmentId",
                table: "photo_evidence");

            migrationBuilder.AddColumn<long>(
                name: "ByteLength",
                table: "photo_evidence",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CloudinaryVersion",
                table: "photo_evidence",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "photo_evidence",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "photo_evidence",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observation",
                table: "photo_evidence",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "photo_evidence",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "photo_evidence",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedById",
                table: "photo_evidence",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedByName",
                table: "photo_evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "photo_evidence",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_photo_evidence_Id_EquipmentId",
                table: "photo_evidence",
                columns: new[] { "Id", "EquipmentId" });

            migrationBuilder.CreateTable(
                name: "photo_principal_periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_principal_periods", x => x.Id);
                    table.CheckConstraint("CK_photo_principal_period", "\"ToUtc\" IS NULL OR \"ToUtc\" > \"FromUtc\"");
                    table.ForeignKey(
                        name: "FK_photo_principal_periods_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_photo_principal_periods_photo_evidence_PhotoId_EquipmentId",
                        columns: x => new { x.PhotoId, x.EquipmentId },
                        principalTable: "photo_evidence",
                        principalColumns: new[] { "Id", "EquipmentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photo_uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Format = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    ByteLength = table.Column<long>(type: "bigint", nullable: true),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_uploads", x => x.Id);
                    table.CheckConstraint("CK_photo_upload_completed", "(\"CompletedAtUtc\" IS NULL) = (\"EvidenceId\" IS NULL)");
                    table.CheckConstraint("CK_photo_upload_metadata", "\"Sha256\" IS NULL OR (\"Sha256\" ~ '^[0-9A-F]{64}$' AND \"Format\" IS NOT NULL AND \"Width\" IS NOT NULL AND \"Height\" IS NOT NULL AND \"ByteLength\" IS NOT NULL AND \"Format\" IN ('jpg','png') AND \"Width\" > 0 AND \"Height\" > 0 AND \"Width\"::bigint * \"Height\" <= 25000000 AND \"ByteLength\" BETWEEN 1 AND 10485760)");
                    table.CheckConstraint("CK_photo_upload_position", "\"Position\" BETWEEN 0 AND 9");
                    table.CheckConstraint("CK_photo_upload_source", "\"Source\" IN ('Archivo','Camara')");
                    table.CheckConstraint("CK_photo_upload_type", "\"EvidenceType\" IN ('General','Serial','Accesorio')");
                    table.ForeignKey(
                        name: "FK_photo_uploads_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_photo_uploads_photo_evidence_EvidenceId_EquipmentId",
                        columns: x => new { x.EvidenceId, x.EquipmentId },
                        principalTable: "photo_evidence",
                        principalColumns: new[] { "Id", "EquipmentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_EquipmentId_Sha256",
                table: "photo_evidence",
                columns: new[] { "EquipmentId", "Sha256" },
                unique: true,
                filter: "\"Sha256\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_EquipmentId_UploadedAtUtc_Id",
                table: "photo_evidence",
                columns: new[] { "EquipmentId", "UploadedAtUtc", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_photo_evidence_metadata",
                table: "photo_evidence",
                sql: "\"Sha256\" IS NULL OR (\"Sha256\" ~ '^[0-9A-F]{64}$' AND \"Format\" IS NOT NULL AND \"Width\" IS NOT NULL AND \"Height\" IS NOT NULL AND \"ByteLength\" IS NOT NULL AND \"CloudinaryVersion\" IS NOT NULL AND \"Format\" IN ('jpg','png') AND \"Width\" > 0 AND \"Height\" > 0 AND \"Width\"::bigint * \"Height\" <= 25000000 AND \"ByteLength\" BETWEEN 1 AND 10485760 AND \"CloudinaryVersion\" > 0 AND \"UploadedById\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_photo_principal_periods_EquipmentId",
                table: "photo_principal_periods",
                column: "EquipmentId",
                unique: true,
                filter: "\"ToUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_photo_principal_periods_PhotoId_EquipmentId",
                table: "photo_principal_periods",
                columns: new[] { "PhotoId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_photo_uploads_EquipmentId_ActorId_BatchId_Position",
                table: "photo_uploads",
                columns: new[] { "EquipmentId", "ActorId", "BatchId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_uploads_EvidenceId_EquipmentId",
                table: "photo_uploads",
                columns: new[] { "EvidenceId", "EquipmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A schema rollback must never silently discard photo history or upload recovery records.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM photo_principal_periods)
                     OR EXISTS (SELECT 1 FROM photo_uploads)
                     OR EXISTS (SELECT 1 FROM photo_evidence WHERE "Sha256" IS NOT NULL)
                  THEN RAISE EXCEPTION 'No se puede revertir la etapa A con evidencias o reservas registradas.';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "photo_principal_periods");

            migrationBuilder.DropTable(
                name: "photo_uploads");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_photo_evidence_Id_EquipmentId",
                table: "photo_evidence");

            migrationBuilder.DropIndex(
                name: "IX_photo_evidence_EquipmentId_Sha256",
                table: "photo_evidence");

            migrationBuilder.DropIndex(
                name: "IX_photo_evidence_EquipmentId_UploadedAtUtc_Id",
                table: "photo_evidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_photo_evidence_metadata",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "ByteLength",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "CloudinaryVersion",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Observation",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "UploadedById",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "UploadedByName",
                table: "photo_evidence");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "photo_evidence");

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_EquipmentId",
                table: "photo_evidence",
                column: "EquipmentId");
        }
    }
}
