# Running MovieManager locally

This guide takes you from a fresh clone to the app running with the OMDb import working.
All commands run from the repository root.

## 1. Prerequisites

| Tool | Notes |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | `global.json` asks for `10.0.300` or a newer .NET 10 feature band. Check with `dotnet --version`. |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Runs the database. It must be **running** (not just installed) before step 2. |
| An IDE (optional) | Rider, Visual Studio or VS Code. |
| A database client (optional) | For example DBeaver, to look at the tables. |

## 2. Start the database

```bash
docker compose up -d
docker compose ps        # the "sqlserver" container should say "Up"
```

This starts Azure SQL Edge on `localhost:1433`. The data is stored in `./data`, which is
gitignored, so it survives restarts. The container doesn't start by itself after a reboot,
so run `docker compose up -d` again then.

## 3. Create the schema

```bash
dotnet tool restore
dotnet ef database update --project Infrastructure --startup-project Web
```

`dotnet tool restore` installs the pinned `dotnet-ef`. The second command creates the
`MovieManager` database and applies every migration. It should end with `Done.`

The first time, if it can't connect, wait a few seconds: SQL Edge takes a moment to accept
connections after the container starts.

## 4. Get an OMDb API key

The importer needs a free key from OMDb.

1. Go to **https://www.omdbapi.com/apikey.aspx**.
2. Choose **FREE!** (1,000 requests per day).
3. Fill in the form. The name and the other details don't have to be your real personal
   data, but the **email must be one you can read**, because the key is sent there.
4. Submit, then open the email from OMDb. It contains your key (8 characters) and an
   **activation link**. Click the link: the key doesn't work until it's activated.

## 5. Store the key in user secrets

The app reads the key from the configuration setting `Omdb:ApiKey`, which lives in the
**user secrets** of the `Web` project. User secrets are stored in your user profile,
outside the repository, so the key is never committed.

```bash
dotnet user-secrets set "Omdb:ApiKey" "<your-key>" --project Web
dotnet user-secrets list --project Web     # should show Omdb:ApiKey = <your-key>
```

> A `.env` file is **not** read by .NET. If you keep the key in `.env` for other tools, you
> still have to set it in user secrets as shown above.

Without a key the app still runs. Only the importer shows "The OMDb API key is not
configured (Omdb:ApiKey)."

## 6. Run the app

From the command line:

```bash
dotnet run --project Web/Web.csproj --launch-profile Web
```

Or in Rider or Visual Studio, pick the **Web** run configuration and press Run.

The app opens at **https://localhost:7211** (http://localhost:5164 also works). The **Web**
profile runs in Development and seeds demo data on startup: The Matrix, Inception, John Wick,
The Shawshank Redemption, their actors and three categories.

If the browser warns about the certificate, trust the .NET development certificate once:

```bash
dotnet dev-certs https --trust
```

## 7. Import a movie

1. Click **Import from OMDb** in the menu (or go to https://localhost:7211/import).
2. Search a title, for example `arrival`. You'll see up to 5 results with poster, title and year.
3. Click **Import** on one of them. You land on the movies list, where the new movie shows
   its rating and actors.

Things the importer refuses, by design:

| Situation | Message |
|---|---|
| Empty search | "Enter a title to search." |
| The movie was already imported | "This movie has already been imported." |
| A different movie with the same title exists (for example the seeded The Matrix) | "A movie titled '…' already exists." |

## 8. Look at the database (optional)

| Setting | Value |
|---|---|
| Driver | SQL Server |
| Host / Port | `localhost` / `1433` |
| Database | `MovieManager` |
| User / Password | `sa` / `Password1` (from `docker-compose.yml`, local only) |
| Driver property | `trustServerCertificate = true` (the container uses a self-signed certificate) |

The tables are in `dbo`: `Movies`, `Actors`, `Categories`, `MovieActors` and
`__EFMigrationsHistory`.

## 9. Run the tests

```bash
dotnet build
dotnet test
```

The tests use saved OMDb responses and the EF Core InMemory provider, so they need neither
the key nor the database. To also run the SQL integration tests against your local database:

```bash
# bash
export MOVIEMANAGER_SQL_TEST_CONNECTION="Server=localhost,1433;Database=MovieManager;User Id=sa;Password=Password1;TrustServerCertificate=True;Encrypt=True"
dotnet test
```

```powershell
# PowerShell
$env:MOVIEMANAGER_SQL_TEST_CONNECTION = "Server=localhost,1433;Database=MovieManager;User Id=sa;Password=Password1;TrustServerCertificate=True;Encrypt=True"
dotnet test
```

## Troubleshooting

| Problem | What to do |
|---|---|
| `failed to connect to the docker API` | Docker Desktop isn't running. Start it, wait until it says it's running, then repeat step 2. |
| Port `1433` is already in use | Another SQL Server is running. Stop it, or change the left side of `"1433:1433"` in `docker-compose.yml` and the port in `Web/appsettings.Development.json`. |
| `dotnet ef` can't connect, or there's a login error right after `docker compose up` | SQL Edge is still starting. Wait 10–20 seconds and retry. |
| Port `7211` is already in use | Another copy of the app is running (for example one started from the terminal and one from the IDE). Stop one of them. |
| "The OMDb API key is not configured (Omdb:ApiKey)." | Do step 5, then restart the app. |
| "The OMDb API key is invalid." | Check the key for typos, and make sure you clicked the activation link in the email. |
| "The OMDb daily request limit has been reached. Try again tomorrow." | The free key allows 1,000 requests per day. Wait until the next day. |
| "Could not reach OMDb. Check your connection." | No internet access, or omdbapi.com is down. |
| You want a clean database | `docker compose down`, delete the `data` folder, `docker compose up -d`, then repeat step 3. The demo data comes back the next time the app starts. |
