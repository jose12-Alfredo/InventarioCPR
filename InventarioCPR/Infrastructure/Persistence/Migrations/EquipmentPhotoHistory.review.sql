START TRANSACTION;
DROP INDEX "IX_photo_evidence_EquipmentId";

ALTER TABLE photo_evidence ADD "ByteLength" bigint;

ALTER TABLE photo_evidence ADD "CloudinaryVersion" bigint;

ALTER TABLE photo_evidence ADD "Format" character varying(8);

ALTER TABLE photo_evidence ADD "Height" integer;

ALTER TABLE photo_evidence ADD "Observation" character varying(2000);

ALTER TABLE photo_evidence ADD "Sha256" character varying(64);

ALTER TABLE photo_evidence ADD "Source" character varying(20);

ALTER TABLE photo_evidence ADD "UploadedById" uuid;

ALTER TABLE photo_evidence ADD "UploadedByName" character varying(256);

ALTER TABLE photo_evidence ADD "Width" integer;

ALTER TABLE photo_evidence ADD CONSTRAINT "AK_photo_evidence_Id_EquipmentId" UNIQUE ("Id", "EquipmentId");

CREATE TABLE photo_principal_periods (
    "Id" uuid NOT NULL,
    "EquipmentId" uuid NOT NULL,
    "PhotoId" uuid NOT NULL,
    "FromUtc" timestamp with time zone NOT NULL,
    "ToUtc" timestamp with time zone,
    "ActorId" uuid NOT NULL,
    "ActorName" character varying(256) NOT NULL,
    CONSTRAINT "PK_photo_principal_periods" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_photo_principal_period" CHECK ("ToUtc" IS NULL OR "ToUtc" > "FromUtc"),
    CONSTRAINT "FK_photo_principal_periods_equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES equipment ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_photo_principal_periods_photo_evidence_PhotoId_EquipmentId" FOREIGN KEY ("PhotoId", "EquipmentId") REFERENCES photo_evidence ("Id", "EquipmentId") ON DELETE RESTRICT
);

CREATE TABLE photo_uploads (
    "Id" uuid NOT NULL,
    "EquipmentId" uuid NOT NULL,
    "BatchId" uuid NOT NULL,
    "Position" integer NOT NULL,
    "ActorId" uuid NOT NULL,
    "ActorName" character varying(256) NOT NULL,
    "FileName" character varying(200) NOT NULL,
    "EvidenceType" character varying(40) NOT NULL,
    "Note" character varying(2000) NOT NULL,
    "Source" character varying(20) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "Sha256" character varying(64),
    "Format" character varying(8),
    "Width" integer,
    "Height" integer,
    "ByteLength" bigint,
    "EvidenceId" uuid,
    CONSTRAINT "PK_photo_uploads" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_photo_upload_completed" CHECK (("CompletedAtUtc" IS NULL) = ("EvidenceId" IS NULL)),
    CONSTRAINT "CK_photo_upload_metadata" CHECK ("Sha256" IS NULL OR ("Sha256" ~ '^[0-9A-F]{64}$' AND "Format" IS NOT NULL AND "Width" IS NOT NULL AND "Height" IS NOT NULL AND "ByteLength" IS NOT NULL AND "Format" IN ('jpg','png') AND "Width" > 0 AND "Height" > 0 AND "Width"::bigint * "Height" <= 25000000 AND "ByteLength" BETWEEN 1 AND 10485760)),
    CONSTRAINT "CK_photo_upload_position" CHECK ("Position" BETWEEN 0 AND 9),
    CONSTRAINT "CK_photo_upload_source" CHECK ("Source" IN ('Archivo','Camara')),
    CONSTRAINT "CK_photo_upload_type" CHECK ("EvidenceType" IN ('General','Serial','Accesorio')),
    CONSTRAINT "FK_photo_uploads_equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES equipment ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_photo_uploads_photo_evidence_EvidenceId_EquipmentId" FOREIGN KEY ("EvidenceId", "EquipmentId") REFERENCES photo_evidence ("Id", "EquipmentId") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_photo_evidence_EquipmentId_Sha256" ON photo_evidence ("EquipmentId", "Sha256") WHERE "Sha256" IS NOT NULL;

CREATE INDEX "IX_photo_evidence_EquipmentId_UploadedAtUtc_Id" ON photo_evidence ("EquipmentId", "UploadedAtUtc", "Id");

ALTER TABLE photo_evidence ADD CONSTRAINT "CK_photo_evidence_metadata" CHECK ("Sha256" IS NULL OR ("Sha256" ~ '^[0-9A-F]{64}$' AND "Format" IS NOT NULL AND "Width" IS NOT NULL AND "Height" IS NOT NULL AND "ByteLength" IS NOT NULL AND "CloudinaryVersion" IS NOT NULL AND "Format" IN ('jpg','png') AND "Width" > 0 AND "Height" > 0 AND "Width"::bigint * "Height" <= 25000000 AND "ByteLength" BETWEEN 1 AND 10485760 AND "CloudinaryVersion" > 0 AND "UploadedById" IS NOT NULL));

CREATE UNIQUE INDEX "IX_photo_principal_periods_EquipmentId" ON photo_principal_periods ("EquipmentId") WHERE "ToUtc" IS NULL;

CREATE INDEX "IX_photo_principal_periods_PhotoId_EquipmentId" ON photo_principal_periods ("PhotoId", "EquipmentId");

CREATE UNIQUE INDEX "IX_photo_uploads_EquipmentId_ActorId_BatchId_Position" ON photo_uploads ("EquipmentId", "ActorId", "BatchId", "Position");

CREATE INDEX "IX_photo_uploads_EvidenceId_EquipmentId" ON photo_uploads ("EvidenceId", "EquipmentId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261109161923_EquipmentPhotoHistory', '10.0.9');

COMMIT;
