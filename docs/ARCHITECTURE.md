# Architecture overview

This document summarizes the project's architecture and domain modelling decisions.

## Layers

- Domain: core business concepts and invariants. Domain entities (Inspection, InspectionTemplate, User) contain behavior and enforce invariants (e.g., EnsureAvailable, Complete, AddOrReplacePhoto).
- Application: use cases and orchestration (AuthService, InspectionService). Accepts DTOs and interacts with abstractions (IAppDbContext, IEmailSender, IFileStorage, ITokenService).
- Infrastructure: implementations (EF Core DbContext, SMTP email sender, file storage). Implements Application abstractions.
- Api: HTTP surface, controllers and middleware. Controllers are intentionally thin and delegate to Application services.

## DDD considerations

- Entities are encapsulated and contain behavior instead of being anemic — good alignment with DDD.
- Aggregates: Inspection acts as an aggregate root for InspectionPhoto entities; InspectionTemplate owns TemplatePhotoRequirement entities.
- Repositories are represented by IAppDbContext abstraction that exposes IQueryable sets; this is a pragmatic approach for EF Core but consider adding repository interfaces if you need richer domain-specific queries or to isolate EF usage further.

## Recent refactor: SMTP sending centralization

What changed:
- Introduced `SmtpSettings` (DTO) to group SMTP configuration.
- Introduced `SmtpClientHelper` to encapsulate MailKit connection/auth/send/disconnect logic.
- Refactored `SmtpEmailSender` to use the helper and avoid duplicated connect/auth/send code across multiple email sending methods.

Benefits:
- Single place to manage SMTP connection behavior and retries in the future.
- Easier to test SmtpEmailSender by mocking the helper or extracting an interface if needed.

## Recommendations

- Add an `Abstractions` project if you plan to reuse interfaces outside this solution or reduce coupling between Application and Infrastructure.
- Add unit tests for Application services and integration tests for persistence and email sending.
- Consider extracting an email sending adapter interface to support queueing or different providers (SendGrid, SES) without touching application logic.
