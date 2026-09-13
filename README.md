# Vistor.ia API

API REST em **.NET 8 + PostgreSQL**, estruturada em Domain, Application, Infrastructure e API. Permite criar modelos de vistoria, definir fotos obrigatórias e enviar um link seguro para uma pessoa preencher a vistoria **sem cadastro**.

O projeto usa EF Core somente como ORM. Não utiliza Code First, migrations,
`EnsureCreated` ou Fluent API para gerar a estrutura. As entidades são mapeadas
com Data Annotations e o banco deve ser criado pela query completa em
`database/001_initial.sql`.

## Executar

Pré-requisito: Docker Desktop.

```bash
docker compose up --build
```

- Swagger: http://localhost:5080/swagger
- Caixa de e-mails local (Mailpit): http://localhost:8025
- PostgreSQL: `localhost:5432`, banco `vistoria`, usuário/senha `postgres`

O Docker executa automaticamente `database/001_initial.sql`. Se o volume já existir e você quiser aplicar o script manualmente:

```bash
docker compose exec -T postgres psql -U postgres -d vistoria < database/001_initial.sql
```

## Fluxo rápido pelo Swagger

1. `POST /api/auth/register` e copie `accessToken`.
2. Clique em **Authorize** e informe `Bearer SEU_TOKEN`.
3. `POST /api/templates` para criar um modelo.
4. `POST /api/inspections` para criar a vistoria; a resposta contém a URL e o token ao final dela.
   Use `GET /api/inspections` para acompanhar todas as vistorias e seus status.
5. `POST /api/inspections/{id}/send`, enviando o token em `publicToken`. O e-mail aparecerá no Mailpit.
6. Sem JWT, consulte `GET /api/public/inspections/{token}`.
7. Envie cada foto via `POST /api/public/inspections/{token}/photos/{requirementId}` (`multipart/form-data`, campo `photo`).
8. Conclua em `POST /api/public/inspections/{token}/complete`.

### Exemplo de modelo

```json
{
  "name": "Vistoria veicular básica",
  "category": "Veículo",
  "description": "Registro visual antes da entrega",
  "photoRequirements": [
    { "code": "frente", "label": "Foto frontal", "required": true, "sortOrder": 1 },
    { "code": "traseira", "label": "Foto traseira", "required": true, "sortOrder": 2 },
    { "code": "painel", "label": "Painel e quilometragem", "required": true, "sortOrder": 3 }
  ]
}
```

### Exemplo de vistoria

```json
{
  "templateId": "UUID_DO_MODELO",
  "recipientName": "Maria Silva",
  "recipientEmail": "maria@email.com",
  "assetIdentification": "ABC1D23",
  "linkValidDays": 7
}
```

## Segurança e decisões

- O banco guarda somente SHA-256 do token público; o token original aparece apenas na criação/envio.
- Tokens têm 256 bits aleatórios e prazo de 1 a 30 dias.
- Senhas usam o `PasswordHasher` oficial do ASP.NET Core (PBKDF2).
- Rotas administrativas exigem JWT e filtram dados por proprietário.
- Upload: JPEG, PNG ou WEBP, no máximo 10 MB; o nome original não é usado no armazenamento.
- A conclusão valida todas as fotos obrigatórias e não permite alterar uma vistoria concluída.
- `regenerate-link` cria uma nova solicitação e invalida operacionalmente o fluxo anterior sem perder auditoria.

## Produção

Troque obrigatoriamente `Jwt__Key`, credenciais do banco e SMTP. Use HTTPS, armazenamento de objetos (S3/Azure Blob), antivírus/validação por assinatura binária nas fotos, rate limiting no endpoint público e um gerenciador de segredos. Ajuste `App__FrontendUrl` para a página web que consumirá o token.

## Estrutura

```text
src/VistoriaApi.Domain          Entidades e invariantes
src/VistoriaApi.Application     Casos de uso, DTOs e interfaces
src/VistoriaApi.Infrastructure  EF Core, PostgreSQL, JWT, SMTP e arquivos
src/VistoriaApi.Api             Controllers, Swagger e middleware
database/001_initial.sql         Estrutura completa do banco
```
