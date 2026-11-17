using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioCPR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompaniesAndEquipmentCustodies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanReceiveEquipment",
                table: "staff_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "staff_members",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCode",
                table: "staff_members",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "staff_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "sites",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "locations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalProfile",
                table: "equipment_categories",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustodyReviewedAtUtc",
                table: "equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsCustodyReconciliation",
                table: "equipment",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("ALTER TABLE equipment ALTER COLUMN \"NeedsCustodyReconciliation\" SET DEFAULT FALSE;");
            migrationBuilder.Sql("UPDATE equipment_categories SET \"TechnicalProfile\"='Computer' WHERE \"Name\" IN ('Computadora de escritorio','Laptop');");

            migrationBuilder.AddColumn<int>(
                name: "OperationsRevision",
                table: "equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerAreaId",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerCompanyId",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsingCompanyId",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "departments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "areas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NameNormalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "upper(btrim(\"Name\"))", stored: true),
                    LegalName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    TaxIdentifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                    table.CheckConstraint("CK_company_name", "length(btrim(\"Name\")) > 0 AND \"Revision\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "equipment_custodies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SharedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Closure = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsInitialReconciliation = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalDeliveryAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WorkerName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    WorkerAreaName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    WorkerDepartmentName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    OwnerCompanyName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    UsingCompanyName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    OwnerAreaName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    SharedLocationName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    SharedSiteName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_custodies", x => x.Id);
                    table.UniqueConstraint("AK_equipment_custodies_Id_EquipmentId", x => new { x.Id, x.EquipmentId });
                    table.CheckConstraint("CK_custody_kind", "(\"Kind\"='Individual' AND \"SharedLocationId\" IS NULL) OR (\"Kind\"='SharedOffice' AND \"SharedLocationId\" IS NOT NULL)");
                    table.CheckConstraint("CK_custody_original_date", "\"OriginalDeliveryAtUtc\" IS NULL OR \"OriginalDeliveryAtUtc\" <= \"StartedAtUtc\"");
                    table.CheckConstraint("CK_custody_period", "(\"EndedAtUtc\" IS NULL AND \"Closure\" IS NULL) OR (\"EndedAtUtc\" IS NOT NULL AND \"EndedAtUtc\">=\"StartedAtUtc\" AND \"Closure\" IN ('Return','Transfer'))");
                    table.ForeignKey(
                        name: "FK_equipment_custodies_areas_WorkerAreaId",
                        column: x => x.WorkerAreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_custodies_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_custodies_locations_SharedLocationId",
                        column: x => x.SharedLocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_custodies_staff_members_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceCustodyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationCustodyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: false),
                    PreviousName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    NewName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipment_operations_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_operations_equipment_custodies_DestinationCustody~",
                        columns: x => new { x.DestinationCustodyId, x.EquipmentId },
                        principalTable: "equipment_custodies",
                        principalColumns: new[] { "Id", "EquipmentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_operations_equipment_custodies_SourceCustodyId_Eq~",
                        columns: x => new { x.SourceCustodyId, x.EquipmentId },
                        principalTable: "equipment_custodies",
                        principalColumns: new[] { "Id", "EquipmentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_PersonnelCode",
                table: "staff_members",
                column: "PersonnelCode",
                unique: true,
                filter: "\"PersonnelCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_OwnerAreaId",
                table: "equipment",
                column: "OwnerAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_OwnerCompanyId_RegistrationState_CodeNormalized",
                table: "equipment",
                columns: new[] { "OwnerCompanyId", "RegistrationState", "CodeNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_UsingCompanyId_RegistrationState",
                table: "equipment",
                columns: new[] { "UsingCompanyId", "RegistrationState" });

            migrationBuilder.CreateIndex(
                name: "IX_companies_NameNormalized",
                table: "companies",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_custodies_EquipmentId",
                table: "equipment_custodies",
                column: "EquipmentId",
                unique: true,
                filter: "\"EndedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_custodies_EquipmentId_StartedAtUtc",
                table: "equipment_custodies",
                columns: new[] { "EquipmentId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_custodies_SharedLocationId",
                table: "equipment_custodies",
                column: "SharedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_custodies_WorkerAreaId",
                table: "equipment_custodies",
                column: "WorkerAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_custodies_WorkerId_EquipmentId",
                table: "equipment_custodies",
                columns: new[] { "WorkerId", "EquipmentId" },
                filter: "\"EndedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_operations_DestinationCustodyId_EquipmentId",
                table: "equipment_operations",
                columns: new[] { "DestinationCustodyId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_operations_EntityType_EntityId_RecordedAtUtc",
                table: "equipment_operations",
                columns: new[] { "EntityType", "EntityId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_operations_EquipmentId_RecordedAtUtc",
                table: "equipment_operations",
                columns: new[] { "EquipmentId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_operations_RequestId",
                table: "equipment_operations",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_operations_SourceCustodyId_EquipmentId",
                table: "equipment_operations",
                columns: new[] { "SourceCustodyId", "EquipmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_areas_OwnerAreaId",
                table: "equipment",
                column: "OwnerAreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_companies_OwnerCompanyId",
                table: "equipment",
                column: "OwnerCompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_companies_UsingCompanyId",
                table: "equipment",
                column: "UsingCompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE FUNCTION protect_equipment_operation_history() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'La auditoría de inventario es inmutable'; END $$;
                CREATE TRIGGER equipment_operations_immutable BEFORE UPDATE OR DELETE ON equipment_operations
                    FOR EACH ROW EXECUTE FUNCTION protect_equipment_operation_history();
                CREATE FUNCTION protect_equipment_custody_history() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'La custodia histórica no se elimina'; END IF;
                    IF OLD."EndedAtUtc" IS NOT NULL OR NEW."EndedAtUtc" IS NULL OR NEW."Closure" IS NULL
                       OR (to_jsonb(NEW) - ARRAY['EndedAtUtc','Closure']) IS DISTINCT FROM (to_jsonb(OLD) - ARRAY['EndedAtUtc','Closure']) THEN
                        RAISE EXCEPTION 'Solo se permite cerrar una custodia vigente';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER equipment_custodies_immutable BEFORE UPDATE OR DELETE ON equipment_custodies
                    FOR EACH ROW EXECUTE FUNCTION protect_equipment_custody_history();
                CREATE FUNCTION validate_equipment_custody_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM 1 FROM equipment WHERE "Id"=NEW."EquipmentId" FOR UPDATE;
                    IF NOT EXISTS(SELECT 1 FROM equipment WHERE "Id"=NEW."EquipmentId" AND "RegistrationState"='Definitive') THEN
                        RAISE EXCEPTION 'Una custodia requiere un activo definitivo';
                    END IF;
                    IF EXISTS(SELECT 1 FROM equipment_custodies c WHERE c."EquipmentId"=NEW."EquipmentId"
                       AND tstzrange(c."StartedAtUtc",c."EndedAtUtc",'[)') && tstzrange(NEW."StartedAtUtc",NEW."EndedAtUtc",'[)')) THEN
                        RAISE EXCEPTION 'El periodo de custodia se solapa con la historia existente';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER equipment_custodies_valid_insert BEFORE INSERT ON equipment_custodies
                    FOR EACH ROW EXECUTE FUNCTION validate_equipment_custody_insert();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS(SELECT 1 FROM equipment_operations) OR EXISTS(SELECT 1 FROM equipment_custodies) OR EXISTS(SELECT 1 FROM companies) THEN
                    RAISE EXCEPTION 'No se puede retirar el módulo con catálogos u operaciones registradas. Revise conservación y respaldo.';
                  END IF;
                END $$;
                """);
            migrationBuilder.Sql("DROP TRIGGER equipment_operations_immutable ON equipment_operations; DROP TRIGGER equipment_custodies_immutable ON equipment_custodies; DROP TRIGGER equipment_custodies_valid_insert ON equipment_custodies; DROP FUNCTION protect_equipment_operation_history(); DROP FUNCTION protect_equipment_custody_history(); DROP FUNCTION validate_equipment_custody_insert();");
            migrationBuilder.DropForeignKey(
                name: "FK_equipment_areas_OwnerAreaId",
                table: "equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_companies_OwnerCompanyId",
                table: "equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_companies_UsingCompanyId",
                table: "equipment");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "equipment_operations");

            migrationBuilder.DropTable(
                name: "equipment_custodies");

            migrationBuilder.DropIndex(
                name: "IX_staff_members_PersonnelCode",
                table: "staff_members");

            migrationBuilder.DropIndex(
                name: "IX_equipment_OwnerAreaId",
                table: "equipment");

            migrationBuilder.DropIndex(
                name: "IX_equipment_OwnerCompanyId_RegistrationState_CodeNormalized",
                table: "equipment");

            migrationBuilder.DropIndex(
                name: "IX_equipment_UsingCompanyId_RegistrationState",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "CanReceiveEquipment",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "PersonnelCode",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "TechnicalProfile",
                table: "equipment_categories");

            migrationBuilder.DropColumn(
                name: "CustodyReviewedAtUtc",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "NeedsCustodyReconciliation",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "OperationsRevision",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "OwnerAreaId",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "OwnerCompanyId",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "UsingCompanyId",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "areas");
        }
    }
}
