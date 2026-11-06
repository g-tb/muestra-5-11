using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

[TestClass]
public class MovieImportServiceTest
{
    private static readonly CatalogMovie Matrix = new(
        "tt0133093", "The Matrix", 1999, "Lana Wachowski, Lilly Wachowski", 8.7m,
        ["Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss"], ["Action", "Sci-Fi"]);

    private static readonly CatalogMovie SecretWars = new(
        "tt21361444", "Avengers: Secret Wars", 2027, "Anthony Russo, Joe Russo", null,
        ["Joseph Quinn", "Vanessa Kirby", "Pedro Pascal"], ["Action", "Adventure", "Sci-Fi"]);

    private FakeRepository<Movie> _movies = null!;
    private FakeRepository<Actor> _actors = null!;
    private FakeRepository<Category> _categories = null!;
    private FakeCatalogClient _catalog = null!;
    private IMovieImportService _importService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _movies = new FakeRepository<Movie>();
        _actors = new FakeRepository<Actor>();
        _categories = new FakeRepository<Category>();
        _catalog = new FakeCatalogClient();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<Movie>>(_movies);
        services.AddSingleton<IRepository<Actor>>(_actors);
        services.AddSingleton<IRepository<Category>>(_categories);
        services.AddSingleton<IMovieCatalogClient>(_catalog);
        services.AddApplication();

        _importService = services.BuildServiceProvider().GetRequiredService<IMovieImportService>();
    }

    [TestMethod]
    public async Task SearchAsync_EmptyQuery_ThrowsLogicExceptionWithoutCallingCatalog()
    {
        var exception = await AssertThrowsAsync<LogicException>(() => _importService.SearchAsync(""));

        Assert.AreEqual("Enter a title to search.", exception.Message);
        Assert.AreEqual(0, _catalog.SearchCalls);
    }

    [TestMethod]
    public async Task SearchAsync_WhitespaceQuery_ThrowsLogicException()
    {
        await AssertThrowsAsync<LogicException>(() => _importService.SearchAsync("   "));

        Assert.AreEqual(0, _catalog.SearchCalls);
    }

    [TestMethod]
    public async Task SearchAsync_NoResults_ReturnsEmptyList()
    {
        IList<MovieSearchResultDTO> results = await _importService.SearchAsync("zzqxjvnotamovie");

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task SearchAsync_MoreThanFiveResults_ReturnsFirstFiveTrimmingTheQuery()
    {
        _catalog.SearchResults = Enumerable.Range(1, 10)
            .Select(i => new CatalogSearchItem($"tt000000{i}", $"Movie {i}", 2000 + i, $"https://posters/{i}.jpg"))
            .ToList();

        IList<MovieSearchResultDTO> results = await _importService.SearchAsync("  matrix ");

        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("tt0000001", results[0].ImdbId);
        Assert.AreEqual("Movie 5", results[4].Title);
        Assert.AreEqual(2005, results[4].Year);
        Assert.AreEqual("matrix", _catalog.LastSearch);
    }

    [TestMethod]
    public async Task SearchAsync_ResultWithoutPoster_KeepsPosterUrlNull()
    {
        _catalog.SearchResults = [new CatalogSearchItem("tt1842414", "Matrix IV", 2010, null)];

        IList<MovieSearchResultDTO> results = await _importService.SearchAsync("matrix");

        Assert.IsNull(results.Single().PosterUrl);
    }

    [TestMethod]
    public async Task SearchAsync_CatalogFails_PropagatesExternalServiceException()
    {
        _catalog.Failure = new ExternalServiceException("The OMDb daily request limit has been reached. Try again tomorrow.");

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => _importService.SearchAsync("matrix"));

        Assert.AreEqual("The OMDb daily request limit has been reached. Try again tomorrow.", exception.Message);
    }

    [TestMethod]
    public async Task ImportAsync_NewMovie_SavesDetailsActorsAndFirstGenre()
    {
        _catalog.Add(Matrix);

        MovieDTO imported = await _importService.ImportAsync("tt0133093");

        Movie stored = _movies.FindAll().Single();
        Assert.AreEqual(imported.Id, stored.Id);
        Assert.AreEqual("The Matrix", stored.Title);
        Assert.AreEqual("Lana Wachowski, Lilly Wachowski", stored.Director);
        Assert.AreEqual(1999, stored.ReleaseYear);
        Assert.AreEqual(8.7m, stored.ImdbRating);
        Assert.AreEqual("tt0133093", stored.ImdbId);
        Assert.AreEqual("Action", stored.Category!.Name);
        CollectionAssert.AreEqual(
            new[] { "Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss" },
            stored.Actors.Select(actor => actor.Name).ToArray());
        Assert.AreEqual(3, _actors.FindAll().Count);
        Assert.AreEqual(1, _categories.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_RatingNotAvailable_SavesWithoutRating()
    {
        _catalog.Add(SecretWars);

        MovieDTO imported = await _importService.ImportAsync("tt21361444");

        Assert.IsNull(imported.ImdbRating);
        Assert.IsNull(_movies.FindAll().Single().ImdbRating);
    }

    [TestMethod]
    public async Task ImportAsync_NoGenreNorActors_SavesWithoutCategoryOrActors()
    {
        _catalog.Add(new CatalogMovie("tt1842414", "Matrix IV", 2010, "Richard Whyte", 7.0m, [], []));

        await _importService.ImportAsync("tt1842414");

        Movie stored = _movies.FindAll().Single();
        Assert.IsNull(stored.Category);
        Assert.AreEqual(0, stored.Actors.Count);
        Assert.AreEqual(0, _categories.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_ActorAndCategoryAlreadyExistWithOtherCaseAndSpaces_ReusesThem()
    {
        Actor keanu = _actors.Add(new Actor { Name = "  KEANU reeves " });
        Category action = _categories.Add(new Category { Name = "action " });
        _catalog.Add(Matrix);

        await _importService.ImportAsync("tt0133093");

        Movie stored = _movies.FindAll().Single();
        Assert.AreEqual(action.Id, stored.Category!.Id);
        Assert.AreEqual(keanu.Id, stored.Actors[0].Id);
        Assert.AreEqual(3, _actors.FindAll().Count);
        Assert.AreEqual(1, _categories.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_SameActorListedTwice_CreatesItOnce()
    {
        _catalog.Add(new CatalogMovie("tt0000001", "Solo", 2020, null, null, ["Ann Lee", " ann lee"], ["Drama"]));

        await _importService.ImportAsync("tt0000001");

        Assert.AreEqual(1, _actors.FindAll().Count);
        Assert.AreEqual(1, _movies.FindAll().Single().Actors.Count);
    }

    [TestMethod]
    public async Task ImportAsync_MovieAlreadyImported_ThrowsWithoutCallingCatalog()
    {
        _movies.Add(new Movie { Title = "The Matrix", ImdbId = "tt0133093" });
        _catalog.Add(Matrix);

        var exception = await AssertThrowsAsync<LogicException>(() => _importService.ImportAsync(" TT0133093 "));

        Assert.AreEqual("This movie has already been imported.", exception.Message);
        Assert.AreEqual(0, _catalog.DetailCalls);
        Assert.AreEqual(1, _movies.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_CalledTwice_SecondCallThrowsAndKeepsOneMovie()
    {
        _catalog.Add(Matrix);
        await _importService.ImportAsync("tt0133093");

        var exception = await AssertThrowsAsync<LogicException>(() => _importService.ImportAsync("tt0133093"));

        Assert.AreEqual("This movie has already been imported.", exception.Message);
        Assert.AreEqual(1, _movies.FindAll().Count);
        Assert.AreEqual(3, _actors.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_DifferentMovieWithExistingTitle_ThrowsAndCreatesNothing()
    {
        _movies.Add(new Movie { Title = " the matrix" });
        _catalog.Add(Matrix);

        var exception = await AssertThrowsAsync<LogicException>(() => _importService.ImportAsync("tt0133093"));

        Assert.AreEqual("A movie titled 'The Matrix' already exists.", exception.Message);
        Assert.AreEqual(1, _movies.FindAll().Count);
        Assert.AreEqual(0, _actors.FindAll().Count);
        Assert.AreEqual(0, _categories.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_UnknownId_ThrowsLogicException()
    {
        var exception = await AssertThrowsAsync<LogicException>(() => _importService.ImportAsync("tt9999999999"));

        Assert.AreEqual("OMDb has no movie with that id.", exception.Message);
        Assert.AreEqual(0, _movies.FindAll().Count);
    }

    [TestMethod]
    public async Task ImportAsync_EmptyId_ThrowsLogicExceptionWithoutCallingCatalog()
    {
        var exception = await AssertThrowsAsync<LogicException>(() => _importService.ImportAsync("  "));

        Assert.AreEqual("Choose a movie to import.", exception.Message);
        Assert.AreEqual(0, _catalog.DetailCalls);
    }

    [TestMethod]
    public async Task ImportAsync_CatalogFails_PropagatesAndSavesNothing()
    {
        _catalog.Failure = new ExternalServiceException("Could not reach OMDb. Check your connection.");

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => _importService.ImportAsync("tt0133093"));

        Assert.AreEqual("Could not reach OMDb. Check your connection.", exception.Message);
        Assert.AreEqual(0, _movies.FindAll().Count);
        Assert.AreEqual(0, _actors.FindAll().Count);
    }

    private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).FullName}.");
        throw new InvalidOperationException("Assert.Fail did not throw.");
    }

    private sealed class FakeCatalogClient : IMovieCatalogClient
    {
        private readonly Dictionary<string, CatalogMovie> _movies = new();

        public List<CatalogSearchItem> SearchResults { get; set; } = [];

        public Exception? Failure { get; set; }

        public int SearchCalls { get; private set; }

        public int DetailCalls { get; private set; }

        public string? LastSearch { get; private set; }

        public void Add(CatalogMovie movie)
        {
            _movies[movie.ImdbId] = movie;
        }

        public Task<IReadOnlyList<CatalogSearchItem>> SearchAsync(string title, CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            LastSearch = title;
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult<IReadOnlyList<CatalogSearchItem>>(SearchResults);
        }

        public Task<CatalogMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
        {
            DetailCalls++;
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(_movies.GetValueOrDefault(imdbId));
        }
    }
}
