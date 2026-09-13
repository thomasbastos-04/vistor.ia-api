BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS vistoria;

CREATE TABLE IF NOT EXISTS vistoria.users (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" varchar(120) NOT NULL,
    "Email" varchar(180) NOT NULL,
    "PasswordHash" varchar(500) NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_users_email UNIQUE ("Email")
);

CREATE TABLE IF NOT EXISTS vistoria.inspection_templates (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "OwnerId" uuid NOT NULL REFERENCES vistoria.users("Id") ON DELETE RESTRICT,
    "Name" varchar(120) NOT NULL,
    "Category" varchar(80) NOT NULL,
    "Description" varchar(500),
    "Active" boolean NOT NULL DEFAULT true,
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS vistoria.template_photo_requirements (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "TemplateId" uuid NOT NULL REFERENCES vistoria.inspection_templates("Id") ON DELETE CASCADE,
    "Code" varchar(60) NOT NULL,
    "Label" varchar(100) NOT NULL,
    "Required" boolean NOT NULL DEFAULT true,
    "SortOrder" integer NOT NULL DEFAULT 0 CHECK ("SortOrder" BETWEEN 0 AND 999),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_requirement_code UNIQUE ("TemplateId", "Code")
);

CREATE TABLE IF NOT EXISTS vistoria.inspections (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "OwnerId" uuid NOT NULL REFERENCES vistoria.users("Id") ON DELETE RESTRICT,
    "TemplateId" uuid NOT NULL REFERENCES vistoria.inspection_templates("Id") ON DELETE RESTRICT,
    "RecipientName" varchar(120) NOT NULL,
    "RecipientEmail" varchar(180) NOT NULL,
    "AssetIdentification" varchar(120) NOT NULL,
    "PublicTokenHash" char(64) NOT NULL,
    "ExpiresAtUtc" timestamptz NOT NULL,
    "Status" integer NOT NULL DEFAULT 1 CHECK ("Status" BETWEEN 1 AND 5),
    "SentAtUtc" timestamptz,
    "CompletedAtUtc" timestamptz,
    "Notes" varchar(2000),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_inspections_token_hash UNIQUE ("PublicTokenHash"),
    CONSTRAINT ck_completed_date CHECK ("Status" <> 4 OR "CompletedAtUtc" IS NOT NULL)
);

CREATE TABLE IF NOT EXISTS vistoria.inspection_photos (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "InspectionId" uuid NOT NULL REFERENCES vistoria.inspections("Id") ON DELETE CASCADE,
    "RequirementId" uuid NOT NULL REFERENCES vistoria.template_photo_requirements("Id") ON DELETE RESTRICT,
    "StoragePath" varchar(500) NOT NULL,
    "ContentType" varchar(100) NOT NULL CHECK ("ContentType" IN ('image/jpeg','image/png','image/webp')),
    "SizeBytes" bigint NOT NULL CHECK ("SizeBytes" > 0 AND "SizeBytes" <= 10485760),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_inspection_requirement UNIQUE ("InspectionId", "RequirementId")
);

CREATE INDEX IF NOT EXISTS ix_templates_owner ON vistoria.inspection_templates("OwnerId", "Active");
CREATE INDEX IF NOT EXISTS ix_inspections_owner_status ON vistoria.inspections("OwnerId", "Status", "CreatedAtUtc" DESC);
CREATE INDEX IF NOT EXISTS ix_inspections_expires ON vistoria.inspections("ExpiresAtUtc") WHERE "Status" IN (1,2,3);
CREATE INDEX IF NOT EXISTS ix_photos_inspection ON vistoria.inspection_photos("InspectionId");
COMMIT;
