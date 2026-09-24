-- Safe index creation for users email column
-- This script will create a case-insensitive index on the first existing
-- email-like column among: email, email_address, user_email.
-- It does nothing if none of these columns exist.

DO $$
DECLARE
    col text;
    idxName text;
    tableName text := 'users';
    schemaName text := 'vistoria';
BEGIN
    SELECT column_name
    INTO col
    FROM information_schema.columns
    WHERE table_schema = schemaName
      AND table_name = tableName
      AND column_name IN ('email', 'email_address', 'user_email')
    ORDER BY CASE column_name
        WHEN 'email' THEN 1
        WHEN 'email_address' THEN 2
        WHEN 'user_email' THEN 3
        ELSE 4 END
    LIMIT 1;

    IF col IS NOT NULL THEN
        idxName := format('idx_users_%s_lower', col);
        EXECUTE format(
            'CREATE INDEX IF NOT EXISTS %I ON %I.%I ((lower(%I)))',
            idxName, schemaName, tableName, col
        );
    END IF;
END
$$;

-- You can run this file safely against your existing DB:
-- psql -h <host> -p <port> -U <user> -d <db> -f database/004_safe_create_idx_users.sql
