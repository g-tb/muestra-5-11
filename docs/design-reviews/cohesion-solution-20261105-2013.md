# Cohesion and coupling review: whole solution (`.`)

Date: 2026-11-05 20:13 · Branch: `develop` (commit `0ba8608`)

**Scope:** every production type in `Domain`, `Application`, `Infrastructure` and
`Web`, including Blazor components with a `@code` block or project-type dependencies.
**Excluded:** test projects, `Infrastructure/Migrations` (generated), and `.cshtml`
views without code (`_Host`, `_Layout`).

**How dependencies are counted:** distinct project types a type references, through
implemented interfaces, constructor parameters, `@inject`, signatures, instantiation or
static calls. .NET, EF Core and ASP.NET types are ignored. Generic arguments count
(`IRepository<Actor>` counts as `IRepository` and `Actor`).

**How duplicates are counted:** a method counts when its logic is repeated in a sibling
type, meaning one that shares an interface or base class with it (`IRepository<T>`,
the `I*Service` trio, or `ComponentBase` pages of the same kind).

---

## Domain

### `IEntity`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Identity contract (`Id`) | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `Actor`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Hold actor state; copy state from another actor (`Update`) | `IEntity` (1) | none (0) | none (see `ActorSqlRepository`) |

Cohesion: High · Coupling: Low

### `Category`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Hold category state; copy state from another category (`Update`) | `IEntity` (1) | none (0) | none (see `CategorySqlRepository`) |

Cohesion: High · Coupling: Low

### `Movie`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Hold movie state; validate `Title`/`Budget` invariants; answer actor membership (`HasActor`); copy state (`Update`) | `IEntity`, `Actor`, `Category`, `DomainException` (4) | none (0) | none |

Cohesion: High · Coupling: Medium (by count only; all dependencies are in its own layer)

### `DomainException`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Signal invalid entity state | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

## Application

### `LogicException`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Signal broken business rule | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `IRepository<TEntity>`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| CRUD contract for persistence | `IEntity` (1) | none (0) | Information Expert: `Find(Func<TEntity,bool>)` takes a compiled delegate, so every implementation has to filter in memory. The database is the expert at filtering, and AGENTS.md requires filters to be translated to SQL. |

Cohesion: High · Coupling: Low

### `ActorDTO`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Carry actor data; map to/from `Actor` | `Actor` (1) | none (0) | none |

Cohesion: High · Coupling: Medium (one concrete class from Domain; required by the `toEntity`/`fromEntity` convention)

### `CategoryDTO`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Carry category data; map to/from `Category` | `Category` (1) | none (0) | none |

Cohesion: High · Coupling: Medium (same reason as `ActorDTO`)

### `MovieDTO`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Carry movie data; map to/from `Movie` (and nested DTOs); answer actor membership (`HasActor`) | `Movie`, `ActorDTO`, `CategoryDTO` (3) | none (0) | Information Expert (ambiguous): `HasActor` repeats `Movie.HasActor`. `Movies/Edit.razor` uses it, and the DTO does own the `Actors` list, so removing it would move the logic into the page. |

Cohesion: High · Coupling: Medium

### `IActorService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Actor use-case contract | `ActorDTO`, `Actor` (2) | none (0) | Naming (AGENTS.md): `AddMovie(Actor movie)` adds an actor. Low Coupling: unlike its siblings, it takes a domain entity instead of a DTO, which makes `Actor/Create.razor` depend on `Domain.Entities.Actor`. |

Cohesion: High · Coupling: Medium (concrete Domain class in a public signature)

### `ICategoryService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Category use-case contract | `CategoryDTO` (1) | none (0) | Naming (AGENTS.md): `AddMovie(CategoryDTO)` adds a category. |

Cohesion: High · Coupling: Low

### `IMovieService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Movie use-case contract | `MovieDTO` (1) | none (0) | none |

Cohesion: High · Coupling: Low

### `ActorService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Delegate actor CRUD to the repository; map entities to DTOs | `IActorService`, `IRepository`, `Actor`, `ActorDTO` (4) | `AddMovie`, `GetAll`, `FindById`, `Update`, `Delete` (5) | none beyond the interface naming |

Cohesion: High · Coupling: Medium

### `CategoryService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Delegate category CRUD to the repository; map entities to DTOs | `ICategoryService`, `IRepository`, `Category`, `CategoryDTO` (4) | `AddMovie`, `GetAll`, `FindById`, `Update`, `Delete` (5) | none beyond the interface naming |

Cohesion: High · Coupling: Medium

### `MovieService`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Delegate movie CRUD; map to DTOs; enforce the unique-title rule | `IMovieService`, `IRepository`, `Movie`, `MovieDTO`, `LogicException` (5) | `GetAll`, `FindById`, `Update`, `Delete` (4) | Information Expert / AGENTS.md: `ValidateMovieName` calls `Find(x => x.Title == ...)`. Because of `IRepository.Find`, on SQL this loads every movie with its category and actors on each add. The comparison rule (case sensitivity) is also implicit rather than stated in the service. |

Cohesion: High · Coupling: High (by count only; all 5 dependencies are abstractions or allowed layers)

### `Application.DependencyInjection`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Register application services | `IMovieService`, `MovieService`, `IActorService`, `ActorService`, `ICategoryService`, `CategoryService` (6) | none (0) | none (knowing the concrete classes is this class's job) |

Cohesion: High · Coupling: High (by count; expected for a composition class)

## Infrastructure

### `MovieManagerDbContext`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Expose `DbSet`s; map the relational schema | `Movie`, `Actor`, `Category` (3) | none (0) | none |

Cohesion: High · Coupling: Medium

### `ActorSqlRepository`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Actor CRUD on SQL; copy actor fields on update | `IRepository`, `Actor`, `MovieManagerDbContext` (3) | `Add`, `Find`, `FindAll`, `Delete`, `Update` skeleton (5) | Information Expert / AGENTS.md: `Find` uses `AsEnumerable()` and loads the whole `Actors` table. Information Expert: `Update` copies `Name`/`Bio`/`BirthDate` by hand, although `Actor.Update` already knows how. |

Cohesion: High · Coupling: Medium

### `CategorySqlRepository`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Category CRUD on SQL; copy category fields on update | `IRepository`, `Category`, `MovieManagerDbContext` (3) | `Add`, `Find`, `FindAll`, `Delete`, `Update` skeleton (5) | Information Expert / AGENTS.md: `Find` loads the whole `Categories` table. Information Expert: `Update` copies `Name` by hand instead of calling `Category.Update`. |

Cohesion: High · Coupling: Medium

### `MovieSqlRepository`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Movie CRUD on SQL; eager-load the movie graph; resolve category and actors to tracked entities | `IRepository`, `Movie`, `Actor`, `Category`, `MovieManagerDbContext` (5) | `Add` (add/save/return tail), `Find`, `Delete` (3) | Information Expert / AGENTS.md: `Find` loads every movie with `Include`s and filters in memory. DRY within the class: `Update` re-implements inline the category/actor resolution that `AttachMovieRelationships` already does for `Add`. |

Cohesion: High · Coupling: High (by count; all dependencies are allowed)

### `DemoDataSeeder`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Define the demo dataset; idempotent get-or-create/upsert per entity; save | `Movie`, `Actor`, `Category`, `MovieManagerDbContext` (4) | none (0) (no siblings; its three lookup methods share a local-then-DB pattern) | SRP (ambiguous): it mixes dataset content with upsert mechanics. This is common and usually fine for a seeder. |

Cohesion: Medium · Coupling: Medium

### `DemoDataSeedingExtensions`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Read the `DemoData:SeedOnStartup` flag; run the seeder in a scope | `DemoDataSeeder` (1) | none (0) | none |

Cohesion: High · Coupling: Low

### `Infrastructure.DependencyInjection`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Configure the `DbContext` from configuration; register the repositories and the seeder | `MovieManagerDbContext`, `DemoDataSeeder`, `IRepository`, `Movie`, `Actor`, `Category`, `MovieSqlRepository`, `ActorSqlRepository`, `CategorySqlRepository` (9) | none (0) | none (composition class) |

Cohesion: High · Coupling: High (by count; expected for a composition class)

## Web

### `Program` (top-level statements)

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Register services; seed at startup; configure the HTTP pipeline | `Application.DependencyInjection`, `Infrastructure.DependencyInjection`, `DemoDataSeedingExtensions` (3) | none (0) | none |

Cohesion: High · Coupling: Medium

### `ErrorModel`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Expose the request id to the error page | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `App`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Routing and default layout | `MainLayout`, `NotFound` (2) | none (0) | none |

Cohesion: High · Coupling: Low

### `MainLayout`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Page shell | `NavMenu` (1) | none (0) | none |

Cohesion: High · Coupling: Low

### `NavMenu`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Render navigation; toggle collapse | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `Index`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Home page links | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `NotFound`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| 404 message | none (0) | none (0) | none |

Cohesion: High · Coupling: Low

### `Pages/Actor/Create`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Bind the create form; save and navigate | `IActorService`, `Actor` (2) | `SaveChanges`, `Cancel` (2) | Low Coupling: it binds to the domain entity `Actor` instead of `ActorDTO` (forced by `IActorService.AddMovie(Actor)`). |

Cohesion: High · Coupling: Medium (concrete Domain class in Web; see ambiguities)

### `Pages/Actor/Edit`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the actor; bind the edit form; save and navigate | `IActorService`, `ActorDTO` (2) | `OnInitialized`, `SaveChanges`, `Cancel` (3) | none |

Cohesion: High · Coupling: Low

### `Pages/Actor/List`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the list; delete and reload | `IActorService`, `ActorDTO` (2) | `OnInitialized`, `DeleteMovie` (2) | Naming: `DeleteMovie` deletes an actor. |

Cohesion: High · Coupling: Low

### `Pages/Category/Create`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Bind the create form; save and navigate | `ICategoryService`, `CategoryDTO` (2) | `SaveChanges`, `Cancel` (2) | none |

Cohesion: High · Coupling: Low

### `Pages/Category/Edit`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the category; bind the edit form; save and navigate | `ICategoryService`, `CategoryDTO` (2) | `OnInitialized`, `SaveChanges`, `Cancel` (3) | none |

Cohesion: High · Coupling: Low

### `Pages/Category/List`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the list; delete and reload | `ICategoryService`, `CategoryDTO` (2) | `OnInitialized`, `DeleteMovie` (2) | Naming: `DeleteMovie` deletes a category. |

Cohesion: High · Coupling: Low

### `Pages/Movies/Create`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Bind the create form; save and navigate | `IMovieService`, `MovieDTO` (2) | `SaveChanges`, `Cancel` (2) | none |

Cohesion: High · Coupling: Low

### `Pages/Movies/Edit`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the movie; load category/actor options; map select events to DTOs; save and navigate | `IMovieService`, `ICategoryService`, `IActorService`, `MovieDTO`, `CategoryDTO`, `ActorDTO` (6) | `SaveChanges`, `Cancel` (2) | Information Expert: `OnChangeCategory` and `OnChangeActors` call `FindById` again, once per selected actor, although `_categories` and `_actors` already hold those objects. |

Cohesion: High · Coupling: High (6 dependencies, all abstractions or DTOs)

### `Pages/Movies/List`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Load the list; delete and reload | `IMovieService`, `MovieDTO` (2) | `OnInitialized`, `DeleteMovie` (2) | none |

Cohesion: High · Coupling: Low

---

## Summary

| Class | Responsibilities | Dependencies | Duplicated methods | Cohesion | Coupling |
|---|---|---|---|---|---|
| `IEntity` | 1 | 0 | 0 | High | Low |
| `Actor` | 2 | 1 | 0 | High | Low |
| `Category` | 2 | 1 | 0 | High | Low |
| `Movie` | 4 | 4 | 0 | High | Medium |
| `DomainException` | 1 | 0 | 0 | High | Low |
| `LogicException` | 1 | 0 | 0 | High | Low |
| `IRepository<T>` | 1 | 1 | 0 | High | Low |
| `ActorDTO` | 2 | 1 | 0 | High | Medium |
| `CategoryDTO` | 2 | 1 | 0 | High | Medium |
| `MovieDTO` | 3 | 3 | 0 | High | Medium |
| `IActorService` | 1 | 2 | 0 | High | Medium |
| `ICategoryService` | 1 | 1 | 0 | High | Low |
| `IMovieService` | 1 | 1 | 0 | High | Low |
| `ActorService` | 2 | 4 | 5 | High | Medium |
| `CategoryService` | 2 | 4 | 5 | High | Medium |
| `MovieService` | 3 | 5 | 4 | High | High |
| `Application.DependencyInjection` | 1 | 6 | 0 | High | High |
| `MovieManagerDbContext` | 2 | 3 | 0 | High | Medium |
| `ActorSqlRepository` | 2 | 3 | 5 | High | Medium |
| `CategorySqlRepository` | 2 | 3 | 5 | High | Medium |
| `MovieSqlRepository` | 3 | 5 | 3 | High | High |
| `DemoDataSeeder` | 3 | 4 | 0 | Medium | Medium |
| `DemoDataSeedingExtensions` | 2 | 1 | 0 | High | Low |
| `Infrastructure.DependencyInjection` | 2 | 9 | 0 | High | High |
| `Program` | 3 | 3 | 0 | High | Medium |
| `ErrorModel` | 1 | 0 | 0 | High | Low |
| `App` | 1 | 2 | 0 | High | Low |
| `MainLayout` | 1 | 1 | 0 | High | Low |
| `NavMenu` | 2 | 0 | 0 | High | Low |
| `Index` | 1 | 0 | 0 | High | Low |
| `NotFound` | 1 | 0 | 0 | High | Low |
| `Actor/Create` | 2 | 2 | 2 | High | Medium |
| `Actor/Edit` | 2 | 2 | 3 | High | Low |
| `Actor/List` | 2 | 2 | 2 | High | Low |
| `Category/Create` | 2 | 2 | 2 | High | Low |
| `Category/Edit` | 2 | 2 | 3 | High | Low |
| `Category/List` | 2 | 2 | 2 | High | Low |
| `Movies/Create` | 2 | 2 | 2 | High | Low |
| `Movies/Edit` | 4 | 6 | 2 | High | High |
| `Movies/List` | 2 | 2 | 2 | High | Low |
| **Total (40 classes)** | 76 | 91 | 47 | | |

Findings: 17. Classes with Low cohesion or High coupling: 5 (`MovieService`,
`Application.DependencyInjection`, `MovieSqlRepository`,
`Infrastructure.DependencyInjection`, `Movies/Edit`). No class has Low cohesion.

No earlier report exists for this scope, so there is no before/after table.

## Ambiguities

- **Composition classes** (both `DependencyInjection` classes and `Program`): the
  rubric rates them High/Medium on count alone. Depending on concrete classes is their job,
  and AGENTS.md puts that coupling there on purpose.
- **Domain entities count as "a concrete class from another layer"**, so DTOs and
  services get Medium coupling even though AGENTS.md allows `Application -> Domain`.
- **Web -> Domain** (`Actor/Create`): the AGENTS.md dependency graph has no direct
  `Web -> Domain` arrow, but nothing forbids it either. I rated it Medium, not High
  (a broken layer rule).
- **`MovieDTO.HasActor`** duplicates `Movie.HasActor`, but the DTO owns its list, and
  removing the method would push the logic into the page. Keeping it is defensible.
- **`Movie.Update`** cannot replace the field copying in `MovieSqlRepository.Update`:
  it assigns the incoming `Category`/`Actors` references, which EF Core does not track.
- **High coupling from count alone**: `MovieService`, `MovieSqlRepository` and
  `Movies/Edit` depend only on abstractions or allowed layers. Their High comes from
  the number of dependencies, not from a broken rule.

## Out-of-scope observations (not counted)

- `MovieService.FindById`/`Update` pass a possibly null result with `!` into
  `MovieDTO.fromEntity`, which throws `NullReferenceException` for an unknown id.
  `ActorService`/`CategoryService` return `null` from non-nullable return types.
- `Actor` and `Category` do not validate in their setters (AGENTS.md: the domain validates entity state).
- `DemoDataSeeder` compares names with C# `==` on `Local` (case-sensitive) and in SQL
  (collation, case-insensitive).
- `Movie.Budget` is ignored by EF, rejects `null`, and is not in `MovieDTO`.
- `Actor/Create.razor` has the heading "Create Movie".

## Proposed changes

1. **Repositories call the entity's `Update`.** `ActorSqlRepository.Update` and
   `CategorySqlRepository.Update` call `found.Update(updatedEntity)` instead of copying
   fields. *Principle:* Information Expert. *Effect:* −2 findings; ratings unchanged.
   *Note:* the entity `Update` also assigns `Id`, with the same value (the entity was
   found by that id); the tests must confirm EF Core accepts this.
2. **Shared `SqlRepository<TEntity>` base** in `Infrastructure/Persistence` holding `Add`,
   `Find`, `FindAll` and `Delete` through `dbContext.Set<TEntity>()`. The actor and category
   repositories keep only `Update`. `MovieSqlRepository` overrides the query (`LoadMovies`)
   and its own `Delete`. *Principle:* DRY / Low Coupling. *Effect:* about −11 duplicated
   methods (47 → about 36), +1 class; repository coupling stays Medium/High.
3. **Extract `ResolveCategory`/`ResolveActors` in `MovieSqlRepository`**, used by both
   `Add` and `Update`. `Update` still clears and refills the tracked collection.
   *Principle:* DRY / SRP within the class. *Effect:* −1 finding; ratings unchanged.
4. **Rename `IActorService.AddMovie(Actor)` → `Add(ActorDTO)` and
   `ICategoryService.AddMovie` → `Add`**, and bind `Actor/Create` to `ActorDTO`.
   `IMovieService.AddMovie` stays, because renaming it would require editing
   `MovieServiceTest`. *Principle:* Low Coupling + naming (AGENTS.md). *Effect:*
   `IActorService` Medium → Low, `Actor/Create` Medium → Low, −3 findings.
5. **Rename `DeleteMovie` → `Delete`** in `Actor/List` and `Category/List`.
   *Principle:* naming clarity. *Effect:* −2 findings.
6. **`Movies/Edit` resolves selections from `_categories`/`_actors`** instead of
   calling `FindById`. *Principle:* Information Expert. *Effect:* −1 finding; coupling
   stays High, because the page still needs the three services for `GetAll`.
   *Caveat:* this is not strictly a refactor. If an actor or category is deleted while
   the page is open, the page would use the loaded copy instead of `null`.
7. **Not applicable under this review: `IRepository.Find(Expression<Func<...>>)`** so
   filters run in SQL. *Principle:* Information Expert + AGENTS.md. *Effect:* −5
   findings. It requires changing `FakeMovieRepository` in `Application.Tests`, and on
   SQL Server it changes the duplicate-title comparison to case-insensitive, which is a
   behavior change. Do it on a `bugfix/` branch with TDD, making the comparison rule
   explicit in `MovieService`.
8. **Not recommended: a generic CRUD service base.** The duplicated service methods
   are one-line delegations. A generic base would need static abstract mapping members
   on the DTOs, which adds more complexity than it removes. *Effect if done:* about −14
   duplicated methods, +1 class.
