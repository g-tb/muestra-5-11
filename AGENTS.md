# AGENTS.md — MovieManager

## Commands

```bash
docker compose up -d                 # SQL Edge on localhost:1433, data in ./data (gitignored)
dotnet tool restore                  # dotnet-ef pinned in dotnet-tools.json
dotnet build
dotnet test
dotnet run --project Web/Web.csproj  # https://localhost:7211 or the URL shown
```

The SDK is pinned in `global.json` (`10.0.300`, roll forward to a newer .NET 10
feature band).

## Architecture

**.NET 10 Blazor Server** app in a Clean Architecture style. Six projects:

1. **Domain**: entities and domain exceptions (`Entities/`, `Exceptions/`).
   References no other project. No repository, service, UI or database concerns.
2. **Application**: services, DTOs, business rules and abstractions
   (`Dtos/`, `Exceptions/`, `Interfaces/`, `Services/`, `DependencyInjection.cs`).
   References `Domain`. Repository and external-service interfaces live in
   `Application/Interfaces`.
3. **Infrastructure**: EF Core repositories against Azure SQL Edge
   (`Persistence/`), external service clients (`External/`), migrations
   (`Migrations/`) and `DependencyInjection.cs`.
   References `Application` and `Domain`.
4. **Web**: Blazor Server UI (`Pages/`, `Shared/`). `Program.cs` is the
   composition root. References `Application` and `Infrastructure`.
5. **Application.Tests**: service tests with repository test doubles.
6. **Infrastructure.Tests**: repository and DI tests with the EF Core InMemory
   provider; SQL integration tests run only when `MOVIEMANAGER_SQL_TEST_CONNECTION`
   is set.

Dependency direction:

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application -> Domain
```

`Domain` references nothing. `Application` never references `Infrastructure` or `Web`.

## Public and internal types

- Public: domain entities and exceptions, DTOs, application exceptions,
  repository and service interfaces, DI extension classes.
- Internal: concrete services and concrete repositories.
- Blazor components use service interfaces only; never repositories or `DbContext`.

## Dependency injection

- Application registrations: `AddApplication(IServiceCollection)`.
- Infrastructure registrations: `AddInfrastructure(IServiceCollection, IConfiguration)`.
- `Web/Program.cs` calls both; it never registers concrete services or repositories.

## Conventions

- **DTO conversion:** DTOs expose `toEntity()` and a static `fromEntity()`; no casting.
- **Validation:** the domain validates entity state in property setters and
  throws `DomainException`; services validate business rules (such as duplicates)
  and throw `LogicException`.
- **Repositories:** interfaces in `Application/Interfaces`, implementations in
  `Infrastructure/Persistence`. The generic `SqlRepository<TEntity>` owns the CRUD
  algorithm (Template Method); entity-specific repositories derive from it and override
  its protected steps (`Query`, `FindTracked`, `PrepareForAdd`, `ApplyChanges`). Entities
  without special loading or relationships are registered with `SqlRepository<TEntity>`
  directly. Query, mapping and transaction logic stays there,
  never in pages. Filters must be translated to SQL; do not load whole tables to
  filter in memory. `IRepository.Find` takes an `Expression<Func<TEntity, bool>>`,
  so filters must stay translatable (no domain methods such as `Movie.HasActor`).
- **Nullable reference types** are enabled in every project; annotate optional
  values (`string?`, `int?`).
- **Language:** write code, comments and commit messages in English.
- Prefer clear names over copying existing mistakes (new methods should not copy
  the `AddMovie` naming pattern of `ActorService`).

## Persistence: Azure SQL Edge

Azure SQL Edge was retired on September 30, 2025; it is used here for local
practice only. The development connection string is in
`Web/appsettings.Development.json` and matches `SA_PASSWORD` in `docker-compose.yml`.

- Do not assume IDs are assigned before save, start at `1`, or have no gaps.
- Do not rely on object reference equality after reads.
- Do not return mutable internal collections from repositories.
- SQL Server compares text case-insensitively by default and the InMemory
  provider does not: make comparison rules explicit in the service instead of
  relying on the database.

## EF Core migrations

The schema is owned by EF Core migrations in `Infrastructure/Migrations`,
generated from `MovieManagerDbContext.OnModelCreating`. No hand-written schema scripts.

```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project Web
dotnet ef database update --project Infrastructure --startup-project Web
```

- Change `OnModelCreating` first, then generate the migration. Never edit the
  model snapshot by hand.
- Review the generated `Up` and `Down` before applying. Watch for dropped columns
  or tables, and renames generated as drop + add.
- Prefer additive changes (new nullable columns, new indexes). Avoid destructive
  changes unless explicitly requested.
- Keep migrations small, one per commit; data backfills go in their own migration.
- Use explicit types, lengths, nullability, indexes and constraint names.
- Validate against a fresh database (`docker compose down`, delete `./data`,
  `docker compose up -d`, update database) before assuming a migration works.

## Secrets and external services

- Connection strings and API keys come from configuration, user secrets or
  environment variables, never from source code or committed files.
- The OMDb API key is read from `Omdb:ApiKey` (user secrets of the `Web` project).
- External service clients are interfaces in `Application/Interfaces` implemented
  in `Infrastructure/External` with a typed `HttpClient`. A missing key, a network
  error or a rate limit must return a clear error, never crash the app: clients throw
  `ExternalServiceException` with a user-readable message.
- The movie catalog port is `IMovieCatalogClient` (implemented by `OmdbClient`). It
  returns clean values (`null` or empty lists, never OMDb's `"N/A"`), so services know
  nothing about HTTP, JSON or OMDb formats.

## Testing

- Follow TDD (Red → Green → Refactor) for new behavior, including work done by
  agents. Commit on Green and on Refactor.
- A refactor does not change behavior: never change a test to make it pass.
- Create fresh dependencies in `[TestInitialize]`.
- Service tests use repository-interface test doubles; repository tests use the
  EF Core InMemory provider.
- Tests never call external APIs; use the saved responses in
  `Infrastructure.Tests/Fixtures`.
- For expected exceptions, use the `AssertThrows` helper pattern from the
  existing tests; use `Assert.AreEqual()` for values.
- Test both happy paths and failures. Test files end with `Test.cs`; test names
  follow `Method_Scenario_ExpectedResult`.
- Run `dotnet build` and `dotnet test` after every change.

## Git workflow (Gitflow)

- Check the current branch before changing code.
- Never work directly on `develop`; use a `feat/...` branch (`bugfix/...` for fixes).
- One focused commit per step, tied to the work performed.
- Use conventional commits (e.g "feat:...", "fix:...", "docs:...")

## Agent skills

Project skills live in `.claude/skills/<name>/SKILL.md`. Check them before
writing new agent-facing instructions.

## Maintenance

Keep this file up to date: if a change touches architecture, stack or
conventions, update it in the same commit.
