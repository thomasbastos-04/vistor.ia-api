<p align="center">
  <img src="docs/assets/vistoria-logo.png" alt="Vistor.ia" width="720">
</p>

<p align="center">
  API para criação, envio e realização de vistorias digitais com evidências fotográficas.
</p>

<p align="center">
  <a href="https://github.com/thomasbastos-04/vistor.ia-api/actions/workflows/ci.yml">
    <img src="https://github.com/thomasbastos-04/vistor.ia-api/actions/workflows/ci.yml/badge.svg" alt="CI">
  </a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8">
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1" alt="PostgreSQL 16">
  <img src="https://img.shields.io/badge/Docker-ready-2496ED" alt="Docker">
  <img src="https://img.shields.io/badge/architecture-Clean%20Architecture-123F36" alt="Clean Architecture">
</p>

## Sobre o projeto

A **Vistor.ia API** é o backend de uma plataforma de vistorias digitais. O sistema permite criar modelos personalizados, definir evidências fotográficas obrigatórias, enviar uma vistoria por link público e acompanhar sua realização sem exigir cadastro do destinatário.

O projeto foi estruturado com **.NET 8**, **Clean Architecture** e conceitos de **Domain-Driven Design**, mantendo regras de negócio isoladas da API, persistência e serviços externos.

## Funcionalidades

- Cadastro e autenticação com JWT.
- Criação e listagem de modelos de vistoria.
- Configuração de exigências fotográficas obrigatórias e opcionais.
- Criação de vistorias vinculadas a um modelo.
- Geração de link público com prazo de expiração.
- Envio de convites por e-mail.
- Realização da vistoria sem cadastro do destinatário.
- Upload de imagens JPEG, PNG e WebP.
- Controle de fotos obrigatórias antes da conclusão.
- Persistência local das evidências com volume Docker.
- Documentação interativa com Swagger/OpenAPI.
- Rotas de saúde para aplicação e PostgreSQL.
- Ambientes separados para desenvolvimento local, Docker e homologação.

## Arquitetura

```text
src/
├── VistoriaApi.Api
│   ├── Authentication
│   ├── Controllers
│   ├── Middlewares
│   └── Requests
├── VistoriaApi.Application
│   ├── Abstractions
│   ├── Contracts
│   ├── Exceptions
│   └── Services
├── VistoriaApi.Domain
│   ├── Common
│   ├── Entities
│   ├── Enums
│   └── Exceptions
└── VistoriaApi.Infrastructure
    ├── Authentication
    ├── Email
    ├── Files
    └── Persistence
```

| Camada | Responsabilidade |
| --- | --- |
| `Domain` | Entidades, estados, invariantes e regras centrais do negócio. |
| `Application` | Casos de uso, contratos e abstrações necessárias à aplicação. |
| `Infrastructure` | PostgreSQL, autenticação, armazenamento de arquivos e SMTP. |
| `Api` | Endpoints HTTP, autorização, Swagger, CORS e tratamento de erros. |

## Tecnologias

- .NET 8 e ASP.NET Core
- Entity Framework Core
- PostgreSQL 16
- JWT Bearer Authentication
- Swagger / OpenAPI
- Docker e Docker Compose
- Mailpit para testes locais de e-mail
- Data Annotations
- GitHub Actions

## Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0), para execução sem Docker.
- [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- Serviço PostgreSQL da Vistor.ia disponível em `localhost:5432`.

O banco é mantido separadamente e criado por script SQL. A aplicação não executa migrations, `EnsureCreated` ou criação automática de tabelas.

## Executar com Docker

Com o PostgreSQL da Vistor.ia já iniciado:

```powershell
Copy-Item .env.example .env
docker compose up --build -d
```

Verifique os containers:

```powershell
docker compose ps
docker compose logs -f api
```

Acessos locais:

| Serviço | Endereço |
| --- | --- |
| API | `http://localhost:5080` |
| Swagger | `http://localhost:5080/swagger` |
| Saúde da API | `http://localhost:5080/health` |
| Saúde do banco | `http://localhost:5080/health/database` |
| Mailpit | `http://localhost:8025` |

Para encerrar:

```powershell
docker compose down
```

## Executar localmente

Com PostgreSQL e Mailpit disponíveis:

```powershell
dotnet restore
dotnet build
dotnet run --project src/VistoriaApi.Api/VistoriaApi.Api.csproj
```

O perfil local utiliza:

```text
http://localhost:5082
```

## Principais rotas

| Método | Rota | Autenticação | Descrição |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Pública | Cadastra um usuário. |
| `POST` | `/api/auth/login` | Pública | Autentica e retorna um JWT. |
| `GET` | `/api/templates` | JWT | Lista os modelos do usuário. |
| `POST` | `/api/templates` | JWT | Cadastra um modelo de vistoria. |
| `GET` | `/api/inspections` | JWT | Lista as vistorias do usuário. |
| `POST` | `/api/inspections` | JWT | Cria uma vistoria e seu link público. |
| `POST` | `/api/inspections/{id}/send` | JWT | Envia o convite por e-mail. |
| `GET` | `/api/public/inspections/{token}` | Pública | Consulta a vistoria pelo link. |
| `POST` | `/api/public/inspections/{token}/photos/{requirementId}` | Pública | Envia uma evidência fotográfica. |
| `POST` | `/api/public/inspections/{token}/complete` | Pública | Conclui a vistoria. |

## Fluxo principal

1. O usuário cria uma conta e faz login.
2. Configura um modelo e suas exigências fotográficas.
3. Cria uma vistoria para um destinatário.
4. Envia o convite contendo um link público temporário.
5. O destinatário acessa o link, envia as fotos e conclui a vistoria.
6. O responsável acompanha o resultado pela plataforma.

## Ambientes

| Ambiente | Configuração |
| --- | --- |
| Desenvolvimento local | `appsettings.Development.json` |
| Docker local | `.env` e `docker-compose.yml` |
| Homologação | `.env.homolog` e `docker-compose.homolog.yml` |
| Produção | Variáveis seguras fornecidas pela infraestrutura |

Arquivos `.env` reais não devem ser versionados. Utilize apenas os exemplos incluídos no repositório.

## Qualidade e segurança

- Senhas armazenadas somente como hash.
- Tokens públicos persistidos apenas como hash.
- Validações de entrada com Data Annotations.
- Limite de 10 MB por fotografia.
- Restrição de formatos de imagem aceitos.
- Isolamento das vistorias por usuário autenticado.
- Links públicos com expiração.
- Segredos fornecidos por variáveis de ambiente.
- Pipeline de integração contínua para build da solução e imagem Docker.

## Roadmap

- Análise automática da qualidade das fotografias.
- Validação das exigências por inteligência artificial.
- Detecção de imagens desfocadas, escuras ou incompatíveis.
- Identificação assistida de possíveis avarias.
- Armazenamento de evidências em serviço de objetos.
- Histórico detalhado e trilha de auditoria.
- Notificações e relatórios da vistoria.

## Autor

Desenvolvido por **Thomas Bastos**.

- GitHub: [thomasbastos-04](https://github.com/thomasbastos-04)
- E-mail: [thomasbastos2004@gmail.com](mailto:thomasbastos2004@gmail.com)

---

<p align="center">
  <strong>Vistor.ia</strong> — vistorias digitais simples, rastreáveis e inteligentes.
</p>
