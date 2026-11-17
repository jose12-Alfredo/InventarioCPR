START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE staff_members ADD "CanReceiveEquipment" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE staff_members ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE staff_members ADD "PersonnelCode" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE staff_members ADD "Revision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE sites ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE sites ADD "Revision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE locations ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE locations ADD "Revision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment_categories ADD "TechnicalProfile" character varying(40) NOT NULL DEFAULT 'General';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "CustodyReviewedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "NeedsCustodyReconciliation" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ALTER COLUMN "NeedsCustodyReconciliation" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    UPDATE equipment_categories SET "TechnicalProfile"='Computer' WHERE "Name" IN ('Computadora de escritorio','Laptop');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "OperationsRevision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "OwnerAreaId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "OwnerCompanyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD "UsingCompanyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE departments ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE departments ADD "Revision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE areas ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE areas ADD "Revision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE TABLE companies (
        "Id" uuid NOT NULL,
        "Name" character varying(160) NOT NULL,
        "NameNormalized" text GENERATED ALWAYS AS (upper(btrim("Name"))) STORED NOT NULL,
        "LegalName" character varying(240),
        "TaxIdentifier" character varying(80),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "Revision" integer NOT NULL,
        CONSTRAINT "PK_companies" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_company_name" CHECK (length(btrim("Name")) > 0 AND "Revision" >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE TABLE equipment_custodies (
        "Id" uuid NOT NULL,
        "EquipmentId" uuid NOT NULL,
        "WorkerId" uuid NOT NULL,
        "WorkerAreaId" uuid NOT NULL,
        "Kind" character varying(30) NOT NULL,
        "SharedLocationId" uuid,
        "StartedAtUtc" timestamp with time zone NOT NULL,
        "EndedAtUtc" timestamp with time zone,
        "Closure" character varying(30),
        "IsInitialReconciliation" boolean NOT NULL,
        "OriginalDeliveryAtUtc" timestamp with time zone,
        "WorkerName" character varying(240) NOT NULL,
        "WorkerAreaName" character varying(240) NOT NULL,
        "WorkerDepartmentName" character varying(240) NOT NULL,
        "OwnerCompanyName" character varying(240),
        "UsingCompanyName" character varying(240),
        "OwnerAreaName" character varying(240),
        "SharedLocationName" character varying(240),
        "SharedSiteName" character varying(240),
        CONSTRAINT "PK_equipment_custodies" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_equipment_custodies_Id_EquipmentId" UNIQUE ("Id", "EquipmentId"),
        CONSTRAINT "CK_custody_kind" CHECK (("Kind"='Individual' AND "SharedLocationId" IS NULL) OR ("Kind"='SharedOffice' AND "SharedLocationId" IS NOT NULL)),
        CONSTRAINT "CK_custody_original_date" CHECK ("OriginalDeliveryAtUtc" IS NULL OR "OriginalDeliveryAtUtc" <= "StartedAtUtc"),
        CONSTRAINT "CK_custody_period" CHECK (("EndedAtUtc" IS NULL AND "Closure" IS NULL) OR ("EndedAtUtc" IS NOT NULL AND "EndedAtUtc">="StartedAtUtc" AND "Closure" IN ('Return','Transfer'))),
        CONSTRAINT "FK_equipment_custodies_areas_WorkerAreaId" FOREIGN KEY ("WorkerAreaId") REFERENCES areas ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_custodies_equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES equipment ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_custodies_locations_SharedLocationId" FOREIGN KEY ("SharedLocationId") REFERENCES locations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_custodies_staff_members_WorkerId" FOREIGN KEY ("WorkerId") REFERENCES staff_members ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE TABLE equipment_operations (
        "Id" uuid NOT NULL,
        "RequestId" uuid NOT NULL,
        "RequestHash" character varying(64) NOT NULL,
        "EntityType" character varying(30) NOT NULL,
        "EntityId" uuid NOT NULL,
        "EquipmentId" uuid,
        "Action" character varying(40) NOT NULL,
        "SourceCustodyId" uuid,
        "DestinationCustodyId" uuid,
        "ActorId" uuid NOT NULL,
        "ActorName" character varying(256) NOT NULL,
        "RecordedAtUtc" timestamp with time zone NOT NULL,
        "EffectiveAtUtc" timestamp with time zone,
        "Note" character varying(4000),
        "BeforeJson" text,
        "AfterJson" text NOT NULL,
        "PreviousName" character varying(240),
        "NewName" character varying(240),
        CONSTRAINT "PK_equipment_operations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_equipment_operations_equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES equipment ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_operations_equipment_custodies_DestinationCustody~" FOREIGN KEY ("DestinationCustodyId", "EquipmentId") REFERENCES equipment_custodies ("Id", "EquipmentId") ON DELETE RESTRICT,
        CONSTRAINT "FK_equipment_operations_equipment_custodies_SourceCustodyId_Eq~" FOREIGN KEY ("SourceCustodyId", "EquipmentId") REFERENCES equipment_custodies ("Id", "EquipmentId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE UNIQUE INDEX "IX_staff_members_PersonnelCode" ON staff_members ("PersonnelCode") WHERE "PersonnelCode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_OwnerAreaId" ON equipment ("OwnerAreaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_OwnerCompanyId_RegistrationState_CodeNormalized" ON equipment ("OwnerCompanyId", "RegistrationState", "CodeNormalized");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_UsingCompanyId_RegistrationState" ON equipment ("UsingCompanyId", "RegistrationState");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE UNIQUE INDEX "IX_companies_NameNormalized" ON companies ("NameNormalized");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE UNIQUE INDEX "IX_equipment_custodies_EquipmentId" ON equipment_custodies ("EquipmentId") WHERE "EndedAtUtc" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_custodies_EquipmentId_StartedAtUtc" ON equipment_custodies ("EquipmentId", "StartedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_custodies_SharedLocationId" ON equipment_custodies ("SharedLocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_custodies_WorkerAreaId" ON equipment_custodies ("WorkerAreaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_custodies_WorkerId_EquipmentId" ON equipment_custodies ("WorkerId", "EquipmentId") WHERE "EndedAtUtc" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_operations_DestinationCustodyId_EquipmentId" ON equipment_operations ("DestinationCustodyId", "EquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_operations_EntityType_EntityId_RecordedAtUtc" ON equipment_operations ("EntityType", "EntityId", "RecordedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_operations_EquipmentId_RecordedAtUtc" ON equipment_operations ("EquipmentId", "RecordedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE UNIQUE INDEX "IX_equipment_operations_RequestId" ON equipment_operations ("RequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    CREATE INDEX "IX_equipment_operations_SourceCustodyId_EquipmentId" ON equipment_operations ("SourceCustodyId", "EquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD CONSTRAINT "FK_equipment_areas_OwnerAreaId" FOREIGN KEY ("OwnerAreaId") REFERENCES areas ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD CONSTRAINT "FK_equipment_companies_OwnerCompanyId" FOREIGN KEY ("OwnerCompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    ALTER TABLE equipment ADD CONSTRAINT "FK_equipment_companies_UsingCompanyId" FOREIGN KEY ("UsingCompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261116181150_CompaniesAndEquipmentCustodies') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261116181150_CompaniesAndEquipmentCustodies', '10.0.9');
    END IF;
END $EF$;
COMMIT;
