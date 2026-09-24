-- Script incremental para adicionar campos de verificação de e-mail na tabela users
-- Use este arquivo se preferir aplicar migração separada da 001_initial.sql

ALTER TABLE IF EXISTS vistoria.users
ADD COLUMN IF NOT EXISTS is_email_verified boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS vistoria.users
ADD COLUMN IF NOT EXISTS email_verification_code varchar(20);

ALTER TABLE IF EXISTS vistoria.users
ADD COLUMN IF NOT EXISTS email_verification_expires_at_utc timestamp with time zone;

-- Índice por e-mail (se ainda não existir) - ajuste conforme necessário
CREATE INDEX IF NOT EXISTS idx_users_email ON vistoria.users (email);

-- Reversão (exemplo)
-- ALTER TABLE vistoria.users DROP COLUMN IF EXISTS email_verification_expires_at_utc;
-- ALTER TABLE vistoria.users DROP COLUMN IF EXISTS email_verification_code;
-- ALTER TABLE vistoria.users DROP COLUMN IF EXISTS is_email_verified;
