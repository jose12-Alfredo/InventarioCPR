START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "DraftPayload" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "FinalizedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "RegistrationRevision" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "RegistrationState" character varying(20) NOT NULL DEFAULT 'Definitive';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "RegistrationUpdatedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD "RequiresRegistrationPhoto" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ALTER COLUMN "RegistrationState" SET DEFAULT 'Draft';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ALTER COLUMN "RequiresRegistrationPhoto" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    CREATE INDEX "IX_equipment_RegistrationState_CodeNormalized" ON equipment ("RegistrationState", "CodeNormalized");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD CONSTRAINT "CK_equipment_draft_size" CHECK ("DraftPayload" IS NULL OR length("DraftPayload") <= 65536);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    ALTER TABLE equipment ADD CONSTRAINT "CK_equipment_registration" CHECK ("RegistrationState" IN ('Draft','Definitive') AND "RegistrationRevision" >= 0 AND ("RegistrationState" <> 'Definitive' OR ("DraftPayload" IS NULL AND (NOT "RequiresRegistrationPhoto" OR "FinalizedAtUtc" IS NOT NULL))));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112200209_EquipmentDraftFinalization') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261112200209_EquipmentDraftFinalization', '10.0.9');
    END IF;
END $EF$;
COMMIT;
