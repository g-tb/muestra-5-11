# Cohesion and coupling review: whole solution (`.`)

Date: 2026-11-05 20:52 · Branch: `feat/repository-template-method` (commit `4b7b7cb`)
Previous report: `cohesion-solution-20261105-2013.md`. Since then only `IRepository`,
the repositories and `Infrastructure.DependencyInjection` changed; every other section
is unchanged.

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
| Hold actor state; copy state from another actor (`Update`) | `IEntity` (1) | none (0) | none (`Update` is no longer used in production; see `SqlRepository`) |

Cohesion: High · Coupling: Low

### `Category`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Hold category state; copy state from another category (`Update`) | `IEntity` (1) | none (0) | none (`Update` is no longer used in production; see `SqlRepository`) |

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
| CRUD contract for persistence | `IEntity` (1) | none (0) | none (`Find` now takes an `Expression`, so filters run in SQL) |

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
| Delegate movie CRUD; map to DTOs; enforce the unique-title rule | `IMovieService`, `IRepository`, `Movie`, `MovieDTO`, `LogicException` (5) | `GetAll`, `FindById`, `Update`, `Delete` (4) | AGENTS.md (persistence): `ValidateMovieName` now runs `Find(x => x.Title == ...)` in SQL, but the comparison rule is still implicit. It is case-insensitive on SQL Server and case-sensitive in the InMemory provider and the test double; it should be stated in the service. |

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

### `SqlRepository<TEntity>` (new)

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Own the CRUD algorithm (Template Method); expose overridable steps (`Query`, `FindTracked`, `PrepareForAdd`, `ApplyChanges`) | `IRepository`, `IEntity`, `MovieManagerDbContext` (3) | none (0) | Information Expert (ambiguous): the default `ApplyChanges` uses `CurrentValues.SetValues`, so `Actor.Update`/`Category.Update` are no longer used in production. `SetValues` may also write backing fields directly and skip setter validation for future entities that use the default. |

Cohesion: High · Coupling: Medium

### `MovieSqlRepository`

| Responsibilities | Dependencies | Duplicated methods | Findings |
|---|---|---|---|
| Eager-load the movie graph; resolve category and actors to tracked entities; copy movie fields | `SqlRepository`, `Movie`, `Actor`, `Category`, `MovieManagerDbContext` (5) | none (0) (it overrides steps; it no longer repeats sibling logic) | none (the earlier in-memory `Find` and the duplicated resolution are fixed) |

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
| Configure the `DbContext` from configuration; register the repositories and the seeder | `MovieManagerDbContext`, `DemoDataSeeder`, `IRepository`, `Movie`, `Actor`, `Category`, `MovieSqlRepository`, `SqlRepository` (8) | none (0) | none (composition class) |

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
| `SqlRepository<T>` | 2 | 3 | 0 | High | Medium |
| `MovieSqlRepository` | 3 | 5 | 0 | High | High |
| `DemoDataSeeder` | 3 | 4 | 0 | Medium | Medium |
| `DemoDataSeedingExtensions` | 2 | 1 | 0 | High | Low |
| `Infrastructure.DependencyInjection` | 2 | 8 | 0 | High | High |
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
| **Total (39 classes)** | 74 | 87 | 34 | | |

Findings: 11. Classes with Low cohesion or High coupling: 5 (`MovieService`,
`Application.DependencyInjection`, `MovieSqlRepository`,
`Infrastructure.DependencyInjection`, `Movies/Edit`). No class has Low cohesion.

### Before / after

| Metric | Before (20:13) | After (20:52) |
|---|---|---|
| Classes | 40 | 39 |
| Duplicated methods | 47 | 34 |
| Findings | 17 | 11 |
| Classes with Low cohesion or High coupling | 5 | 5 |

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

Status of the previous proposals: **2, 3 and 7 are done**. **1 is superseded**: the
default `ApplyChanges` uses `SetValues` instead of the entities' `Update` methods.
**8 is still not recommended.** Pending:

1. **Rename `IActorService.AddMovie(Actor)` to `Add(ActorDTO)` and `ICategoryService.AddMovie` to `Add`**, and bind `Actor/Create` to `ActorDTO`. *Principle:* Low Coupling + naming. *Effect:* `IActorService` and `Actor/Create` go from Medium to Low coupling; −3 findings.
2. **Rename `DeleteMovie` to `Delete`** in `Actor/List` and `Category/List`. *Principle:* naming. *Effect:* −2 findings.
3. **`Movies/Edit` resolves selections from its loaded lists.** *Principle:* Information Expert. *Effect:* −1 finding. *Caveat:* small behavior change if an item is deleted while the page is open.
4. **Make the duplicate-title comparison explicit in `MovieService`** (for example with a normalized or `ToUpper()` comparison that SQL can translate). *Principle:* AGENTS.md persistence rule. *Effect:* −1 finding. *Caveat:* behavior change, so it needs TDD on a `bugfix/` branch.
5. **Decide the role of `Actor.Update`/`Category.Update`:** either call them from `ApplyChanges` overrides or remove them. *Principle:* Information Expert / no dead code. *Effect:* −1 finding.
