using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioCPR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                    table.CheckConstraint("CK_departments_name", "length(btrim(\"Name\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "equipment_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_brands", x => x.Id);
                    table.CheckConstraint("CK_brands_name", "length(btrim(\"Name\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "equipment_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_categories", x => x.Id);
                    table.CheckConstraint("CK_categories_name", "length(btrim(\"Name\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.Id);
                    table.CheckConstraint("CK_sites_name", "length(btrim(\"Name\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.Id);
                    table.CheckConstraint("CK_areas_name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_areas_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category_attribute_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ValueKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_attribute_definitions", x => x.Id);
                    table.UniqueConstraint("AK_category_attribute_definitions_Id_CategoryId", x => new { x.Id, x.CategoryId });
                    table.CheckConstraint("CK_attribute_key", "length(btrim(\"Key\")) > 0");
                    table.CheckConstraint("CK_attribute_revision", "\"Revision\" > 0");
                    table.ForeignKey(
                        name: "FK_category_attribute_definitions_equipment_categories_Categor~",
                        column: x => x.CategoryId,
                        principalTable: "equipment_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_models", x => x.Id);
                    table.UniqueConstraint("AK_commercial_models_Id_BrandId_CategoryId", x => new { x.Id, x.BrandId, x.CategoryId });
                    table.CheckConstraint("CK_models_name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_commercial_models_equipment_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "equipment_brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_models_equipment_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "equipment_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.Id);
                    table.CheckConstraint("CK_locations_name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_locations_sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_members", x => x.Id);
                    table.CheckConstraint("CK_staff_name", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_staff_members_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CodeNormalized = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, computedColumnSql: "upper(btrim(\"Code\"))", stored: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommercialModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SerialAvailability = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "NotRecorded")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.Id);
                    table.CheckConstraint("CK_equipment_code", "length(btrim(\"Code\")) > 0");
                    table.CheckConstraint("CK_equipment_model_brand", "\"CommercialModelId\" IS NULL OR \"BrandId\" IS NOT NULL");
                    table.CheckConstraint("CK_equipment_name", "length(btrim(\"Name\")) > 0");
                    table.CheckConstraint("CK_equipment_serial", "(\"SerialAvailability\" = 'Recorded' AND nullif(btrim(\"SerialNumber\"), '') IS NOT NULL) OR (\"SerialAvailability\" IN ('NotRecorded', 'NotApplicable') AND \"SerialNumber\" IS NULL) OR \"SerialAvailability\" = 'PendingVerification'");
                    table.ForeignKey(
                        name: "FK_equipment_commercial_models_CommercialModelId_BrandId_Categ~",
                        columns: x => new { x.CommercialModelId, x.BrandId, x.CategoryId },
                        principalTable: "commercial_models",
                        principalColumns: new[] { "Id", "BrandId", "CategoryId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_equipment_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "equipment_brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_equipment_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "equipment_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_data_markers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Availability = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_data_markers", x => x.Id);
                    table.CheckConstraint("CK_marker_availability", "\"Availability\" IN ('NotApplicable', 'PendingVerification')");
                    table.CheckConstraint("CK_marker_key", "length(btrim(\"FieldKey\")) > 0 AND \"FieldKey\" <> 'serial_number'");
                    table.ForeignKey(
                        name: "FK_equipment_data_markers_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_inspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PhysicalCondition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperationalCondition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_inspections", x => x.Id);
                    table.UniqueConstraint("AK_equipment_inspections_Id_EquipmentId", x => new { x.Id, x.EquipmentId });
                    table.CheckConstraint("CK_inspection_condition", "\"PhysicalCondition\" IS NOT NULL OR \"OperationalCondition\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_equipment_inspections_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_inspections_staff_members_InspectorId",
                        column: x => x.InspectorId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_location_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_location_observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipment_location_observations_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_location_observations_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_observations", x => x.Id);
                    table.CheckConstraint("CK_observation_text", "length(btrim(\"Text\")) > 0");
                    table.ForeignKey(
                        name: "FK_equipment_observations_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_observations_staff_members_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "technical_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessorManufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProcessorModel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ProcessorDesignation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalRamGb = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    RamModulesComplete = table.Column<bool>(type: "boolean", nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_configurations", x => x.Id);
                    table.UniqueConstraint("AK_technical_configurations_Id_CategoryId", x => new { x.Id, x.CategoryId });
                    table.CheckConstraint("CK_technical_period", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
                    table.CheckConstraint("CK_technical_ram", "\"TotalRamGb\" IS NULL OR \"TotalRamGb\" > 0");
                    table.CheckConstraint("CK_technical_revision", "\"Revision\" > 0");
                    table.ForeignKey(
                        name: "FK_technical_configurations_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_technical_configurations_equipment_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "equipment_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_technical_configurations_staff_members_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "technical_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_technical_verifications_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_technical_verifications_staff_members_VerifierId",
                        column: x => x.VerifierId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photo_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CloudinaryPublicId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_photo_evidence_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_photo_evidence_equipment_inspections_InspectionId_Equipment~",
                        columns: x => new { x.InspectionId, x.EquipmentId },
                        principalTable: "equipment_inspections",
                        principalColumns: new[] { "Id", "EquipmentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ram_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: true),
                    CapacityGb = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Technology = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ram_modules", x => x.Id);
                    table.CheckConstraint("CK_ram_capacity", "\"CapacityGb\" > 0");
                    table.ForeignKey(
                        name: "FK_ram_modules_technical_configurations_TechnicalConfiguration~",
                        column: x => x.TechnicalConfigurationId,
                        principalTable: "technical_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storage_units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    Capacity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    CapacityUnit = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Technology = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Interface = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_units", x => x.Id);
                    table.CheckConstraint("CK_storage_capacity", "(\"Capacity\" IS NULL AND \"CapacityUnit\" IS NULL) OR (\"Capacity\" > 0 AND \"CapacityUnit\" IN ('GB', 'TB'))");
                    table.ForeignKey(
                        name: "FK_storage_units_technical_configurations_TechnicalConfigurati~",
                        column: x => x.TechnicalConfigurationId,
                        principalTable: "technical_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "technical_attribute_values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_attribute_values", x => x.Id);
                    table.CheckConstraint("CK_attribute_one_value", "(CASE WHEN \"TextValue\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"NumberValue\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"BooleanValue\" IS NOT NULL THEN 1 ELSE 0 END) = 1");
                    table.ForeignKey(
                        name: "FK_technical_attribute_values_category_attribute_definitions_D~",
                        columns: x => new { x.DefinitionId, x.CategoryId },
                        principalTable: "category_attribute_definitions",
                        principalColumns: new[] { "Id", "CategoryId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_technical_attribute_values_technical_configurations_Technic~",
                        columns: x => new { x.TechnicalConfigurationId, x.CategoryId },
                        principalTable: "technical_configurations",
                        principalColumns: new[] { "Id", "CategoryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_areas_DepartmentId_Name",
                table: "areas",
                columns: new[] { "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_attribute_definitions_CategoryId_Key_Revision",
                table: "category_attribute_definitions",
                columns: new[] { "CategoryId", "Key", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_models_BrandId_CategoryId_Name",
                table: "commercial_models",
                columns: new[] { "BrandId", "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_models_CategoryId",
                table: "commercial_models",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_Name",
                table: "departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_BrandId",
                table: "equipment",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_CategoryId",
                table: "equipment",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_CodeNormalized",
                table: "equipment",
                column: "CodeNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_CommercialModelId_BrandId_CategoryId",
                table: "equipment",
                columns: new[] { "CommercialModelId", "BrandId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_brands_Name",
                table: "equipment_brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_categories_Name",
                table: "equipment_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_data_markers_EquipmentId_FieldKey",
                table: "equipment_data_markers",
                columns: new[] { "EquipmentId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipment_inspections_EquipmentId_InspectedAtUtc",
                table: "equipment_inspections",
                columns: new[] { "EquipmentId", "InspectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_inspections_InspectorId",
                table: "equipment_inspections",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_location_observations_EquipmentId_ObservedAtUtc",
                table: "equipment_location_observations",
                columns: new[] { "EquipmentId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_location_observations_LocationId",
                table: "equipment_location_observations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_observations_AuthorId",
                table: "equipment_observations",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_observations_EquipmentId_RecordedAtUtc",
                table: "equipment_observations",
                columns: new[] { "EquipmentId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_locations_SiteId_Name",
                table: "locations",
                columns: new[] { "SiteId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_CloudinaryPublicId",
                table: "photo_evidence",
                column: "CloudinaryPublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_EquipmentId",
                table: "photo_evidence",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_evidence_InspectionId_EquipmentId",
                table: "photo_evidence",
                columns: new[] { "InspectionId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ram_modules_TechnicalConfigurationId_Slot",
                table: "ram_modules",
                columns: new[] { "TechnicalConfigurationId", "Slot" },
                unique: true,
                filter: "\"Slot\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sites_Name",
                table: "sites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_AreaId",
                table: "staff_members",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_storage_units_TechnicalConfigurationId_Position",
                table: "storage_units",
                columns: new[] { "TechnicalConfigurationId", "Position" },
                unique: true,
                filter: "\"Position\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_technical_attribute_values_DefinitionId_CategoryId",
                table: "technical_attribute_values",
                columns: new[] { "DefinitionId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_technical_attribute_values_TechnicalConfigurationId_Categor~",
                table: "technical_attribute_values",
                columns: new[] { "TechnicalConfigurationId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_technical_attribute_values_TechnicalConfigurationId_Definit~",
                table: "technical_attribute_values",
                columns: new[] { "TechnicalConfigurationId", "DefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_configurations_CategoryId",
                table: "technical_configurations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_technical_configurations_EquipmentId",
                table: "technical_configurations",
                column: "EquipmentId",
                unique: true,
                filter: "\"EffectiveToUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_technical_configurations_EquipmentId_Revision",
                table: "technical_configurations",
                columns: new[] { "EquipmentId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_configurations_RecordedById",
                table: "technical_configurations",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_technical_verifications_EquipmentId_VerifiedAtUtc",
                table: "technical_verifications",
                columns: new[] { "EquipmentId", "VerifiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_technical_verifications_VerifierId",
                table: "technical_verifications",
                column: "VerifierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_data_markers");

            migrationBuilder.DropTable(
                name: "equipment_location_observations");

            migrationBuilder.DropTable(
                name: "equipment_observations");

            migrationBuilder.DropTable(
                name: "photo_evidence");

            migrationBuilder.DropTable(
                name: "ram_modules");

            migrationBuilder.DropTable(
                name: "storage_units");

            migrationBuilder.DropTable(
                name: "technical_attribute_values");

            migrationBuilder.DropTable(
                name: "technical_verifications");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "equipment_inspections");

            migrationBuilder.DropTable(
                name: "category_attribute_definitions");

            migrationBuilder.DropTable(
                name: "technical_configurations");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "staff_members");

            migrationBuilder.DropTable(
                name: "commercial_models");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "equipment_brands");

            migrationBuilder.DropTable(
                name: "equipment_categories");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
