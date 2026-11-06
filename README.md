# MovieManager

A small **.NET 10 Blazor Server** app for managing movies, actors and categories, built in a
Clean Architecture style. Movies can be created by hand or **imported from
[OMDb](https://www.omdbapi.com/)** with their director, year, IMDb rating, main actors and genre.

It is a teaching project: the code favours clear layers, small services and tests over
features.

## Features

- Create, edit, list and delete **movies**, **actors** and **categories**.
- Assign a category and several actors to each movie.
- **Import from OMDb**: search by title, see the first 5 results with poster, and import one
  by its IMDb id. Existing actors and categories are reused (names match ignoring case and
  surrounding spaces), and a movie can't be imported twice.
- The movies list shows each movie's IMDb rating and actors.
- Demo data (a few movies, actors and categories) is seeded when the app starts in Development.

## Tech stack

| Area | Technology |
|---|---|
| UI | Blazor Server, Bootstrap 5 |
| Backend | .NET 10, C# |
| Data | EF Core 10 with migrations, Azure SQL Edge in Docker |
| External API | OMDb, through a typed `HttpClient` |
| Tests | MSTest, EF Core InMemory provider, saved OMDb responses |

## Quick start

You need the **.NET 10 SDK**, **Docker Desktop** and a free **OMDb API key**.
[docs/running.md](docs/running.md) explains each step, including how to get the key.

```bash
docker compose up -d                                   # SQL Edge on localhost:1433
dotnet tool restore                                    # installs dotnet-ef
dotnet ef database update --project Infrastructure --startup-project Web
dotnet user-secrets set "Omdb:ApiKey" "<your-key>" --project Web
dotnet run --project Web/Web.csproj --launch-profile Web
```

Open https://localhost:7211. In Rider, the **Web** run configuration does the same as the last
command.

## Architecture

```text
Web ──────────────► Application ──► Domain
 └─► Infrastructure ──┘
```

| Project | Responsibility |
|---|---|
| `Domain` | Entities (`Movie`, `Actor`, `Category`) that validate their own state. References nothing. |
| `Application` | Services, DTOs, business rules, and the interfaces of what it needs from outside (`IRepository<T>`, `IMovieCatalogClient`). |
| `Infrastructure` | EF Core repositories and migrations (`Persistence/`, `Migrations/`), and the OMDb client (`External/`). |
| `Web` | Blazor pages. They only call service interfaces. `Program.cs` wires everything up. |
| `Application.Tests` | Service tests with in-memory test doubles. |
| `Infrastructure.Tests` | Repository tests (InMemory provider), the OMDb client tests (saved responses), and optional SQL integration tests. |

How an import flows through the layers:

```text
Import.razor ─► IMovieImportService ─► IMovieCatalogClient ─► OmdbClient ─► omdbapi.com
                      │                  (Application)        (Infrastructure)
                      └─► IRepository<Movie|Actor|Category> ─► SQL Edge
```

The services never see HTTP or JSON. `OmdbClient` turns OMDb's formats (`"N/A"`,
comma-separated lists, error bodies) into clean values. It also turns every failure
(missing or invalid key, daily limit, no network) into an `ExternalServiceException`
with a message the page can show.

The full conventions are in [AGENTS.md](AGENTS.md).

## Tests

```bash
dotnet test
```

- The tests never call the real OMDb API. They read the saved responses in
  `Infrastructure.Tests/Fixtures` ([what each file is](Infrastructure.Tests/Fixtures/README.md)).
- To also run the SQL Server integration tests against your local database, set
  `MOVIEMANAGER_SQL_TEST_CONNECTION` to the connection string in
  `Web/appsettings.Development.json` first.

## Project layout

```text
Domain/            entities and domain exceptions
Application/       services, DTOs, interfaces, application exceptions
Infrastructure/    EF Core (Persistence/, Migrations/) and OMDb client (External/)
Web/               Blazor pages (Pages/), layout and nav (Shared/)
*.Tests/           MSTest projects
docs/              guides (running the app)
docker-compose.yml Azure SQL Edge for local development
```

## Contributing

- Gitflow: branch from `develop` as `feat/...` or `bugfix/...`, and never commit to `develop` directly.
- Conventional commits (`feat:`, `fix:`, `docs:`, ...), one focused commit per step.
- TDD for new behavior: Red → Green → Refactor. Run `dotnet build` and `dotnet test` after every change.
- Schema changes go through EF Core migrations, each in its own commit.
- Never commit secrets. The OMDb key lives in user secrets, not in `appsettings` or `.env`.

> Azure SQL Edge was retired on September 30, 2025. It is used here only for local practice.
