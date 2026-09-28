# Contributing

Thanks for considering contributing to Vistor.ia API. This document explains the expected workflow and coding conventions.

## How to contribute

1. Fork the repository and create a topic branch from `main` or the relevant feature branch.
2. Make focused changes with clear commit messages.
3. Ensure the solution builds: `dotnet build`.
4. Open a pull request against `main` (or the appropriate target branch) with a clear description of changes and why.

## Branching and PRs

- Use short, descriptive branch names: `feature/<what>`, `fix/<what>`, `refactor/<area>`.
- Squash or keep commits logical and small.
- Include in PR description: what changed, why, testing done, and any follow-up tasks.

## Commit message guideline

Use a concise imperative summary. Example:

```
Refactor: centralize SMTP sending and remove duplication

- Add SmtpSettings and SmtpClientHelper to centralize SMTP configuration and sending
- Refactor SmtpEmailSender to use helper and remove duplicated connect/auth/send logic
```

## Code style

- Follow .editorconfig. Keep methods short and focused.
- Prefer expressive names and avoid premature abstractions.
- Keep controllers thin; push business logic to Application services or Domain entities.

## Testing

- Add unit tests for new logic in `src/VistoriaApi.Application`.
- Use mocks for IEmailSender, IFileStorage, and IAppDbContext.

## Review checklist for PRs

- [ ] Build passes (`dotnet build`)
- [ ] No runtime secrets in commits
- [ ] Changes are covered by unit tests (when applicable)
- [ ] Documentation updated (README or docs/) if behavior or setup changed
