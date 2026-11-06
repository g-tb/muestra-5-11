using System.Globalization;
using System.Net;
using System.Text;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

/// <summary>
/// Tests the OMDb client through the real Infrastructure registrations. The HTTP handler is a
/// fake that serves the saved responses in Fixtures/, so the real API is never called.
/// </summary>
[TestClass]
public class OmdbClientTest
{
    private FakeHttpHandler _handler = null!;
    private CultureInfo _originalCulture = null!;

    [TestInitialize]
    public void SetUp()
    {
        _handler = new FakeHttpHandler();
        _originalCulture = CultureInfo.CurrentCulture;
    }

    [TestCleanup]
    public void TearDown()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [TestMethod]
    public async Task SearchAsync_MatchingTitle_MapsResults()
    {
        _handler.RespondWith("search-matrix.json");

        IReadOnlyList<CatalogSearchItem> results = await Client().SearchAsync("matrix");

        Assert.AreEqual(10, results.Count);
        CatalogSearchItem first = results[0];
        Assert.AreEqual("tt0133093", first.ImdbId);
        Assert.AreEqual("The Matrix", first.Title);
        Assert.AreEqual(1999, first.Year);
        Assert.IsTrue(first.PosterUrl!.StartsWith("https://"));
        StringAssert.Contains(_handler.LastRequestUri!.Query, "s=matrix");
        StringAssert.Contains(_handler.LastRequestUri!.Query, "type=movie");
    }

    [TestMethod]
    public async Task SearchAsync_ResultWithoutPoster_HasNullPosterUrl()
    {
        _handler.RespondWith("search-no-poster.json");

        IReadOnlyList<CatalogSearchItem> results = await Client().SearchAsync("matrix");

        Assert.IsNull(results.Single(item => item.ImdbId == "tt1842414").PosterUrl);
    }

    [TestMethod]
    public async Task SearchAsync_NoMatches_ReturnsEmptyList()
    {
        _handler.RespondWith("search-not-found.json");

        IReadOnlyList<CatalogSearchItem> results = await Client().SearchAsync("zzqxjvnotamovie");

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task SearchAsync_TooManyResults_ThrowsExternalServiceException()
    {
        _handler.RespondWith("search-too-many.json");

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().SearchAsync("a"));

        Assert.AreEqual("Too many results; type a more specific title.", exception.Message);
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_KnownMovie_MapsDetails()
    {
        _handler.RespondWith("movie-tt0133093.json");

        CatalogMovie movie = (await Client().GetByImdbIdAsync("tt0133093"))!;

        Assert.AreEqual("tt0133093", movie.ImdbId);
        Assert.AreEqual("The Matrix", movie.Title);
        Assert.AreEqual(1999, movie.Year);
        Assert.AreEqual("Lana Wachowski, Lilly Wachowski", movie.Director);
        Assert.AreEqual(8.7m, movie.Rating);
        CollectionAssert.AreEqual(new[] { "Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss" }, movie.Actors.ToArray());
        CollectionAssert.AreEqual(new[] { "Action", "Sci-Fi" }, movie.Genres.ToArray());
        StringAssert.Contains(_handler.LastRequestUri!.Query, "i=tt0133093");
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_CommaDecimalCulture_ParsesRatingTheSame()
    {
        CultureInfo.CurrentCulture = new CultureInfo("es-CL");
        _handler.RespondWith("movie-tt0133093.json");

        CatalogMovie movie = (await Client().GetByImdbIdAsync("tt0133093"))!;

        Assert.AreEqual(8.7m, movie.Rating);
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_RatingNotAvailable_HasNullRating()
    {
        _handler.RespondWith("movie-rating-na.json");

        CatalogMovie movie = (await Client().GetByImdbIdAsync("tt21361444"))!;

        Assert.IsNull(movie.Rating);
        Assert.AreEqual(2027, movie.Year);
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_ActorsNotAvailable_HasNoActors()
    {
        _handler.RespondWith("movie-no-actors.json");

        CatalogMovie movie = (await Client().GetByImdbIdAsync("tt1842414"))!;

        Assert.AreEqual(0, movie.Actors.Count);
        CollectionAssert.AreEqual(new[] { "Short" }, movie.Genres.ToArray());
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_UnknownId_ReturnsNull()
    {
        _handler.RespondWith("movie-not-found.json");

        Assert.IsNull(await Client().GetByImdbIdAsync("tt9999999999"));
    }

    [TestMethod]
    public async Task SearchAsync_InvalidKey_ThrowsWithClearMessage()
    {
        _handler.RespondWith("error-invalid-key.json", HttpStatusCode.Unauthorized);

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().SearchAsync("matrix"));

        Assert.AreEqual("The OMDb API key is invalid.", exception.Message);
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_DailyLimitReached_ThrowsWithClearMessage()
    {
        _handler.RespondWith("error-limit-reached.json", HttpStatusCode.Unauthorized);

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().GetByImdbIdAsync("tt0133093"));

        Assert.AreEqual("The OMDb daily request limit has been reached. Try again tomorrow.", exception.Message);
    }

    [TestMethod]
    public async Task SearchAsync_MissingKey_ThrowsWithoutCallingOmdb()
    {
        _handler.RespondWith("search-matrix.json");

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client(apiKey: null).SearchAsync("matrix"));

        Assert.AreEqual("The OMDb API key is not configured (Omdb:ApiKey).", exception.Message);
        Assert.AreEqual(0, _handler.RequestCount);
    }

    [TestMethod]
    public async Task SearchAsync_BlankKey_ThrowsWithoutCallingOmdb()
    {
        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client(apiKey: "  ").SearchAsync("matrix"));

        Assert.AreEqual("The OMDb API key is not configured (Omdb:ApiKey).", exception.Message);
        Assert.AreEqual(0, _handler.RequestCount);
    }

    [TestMethod]
    public async Task SearchAsync_NetworkError_ThrowsWithClearMessage()
    {
        _handler.FailWith(new HttpRequestException("No such host is known."));

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().SearchAsync("matrix"));

        Assert.AreEqual("Could not reach OMDb. Check your connection.", exception.Message);
    }

    [TestMethod]
    public async Task GetByImdbIdAsync_Timeout_ThrowsWithClearMessage()
    {
        _handler.FailWith(new TaskCanceledException("The request timed out."));

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().GetByImdbIdAsync("tt0133093"));

        Assert.AreEqual("Could not reach OMDb. Check your connection.", exception.Message);
    }

    [TestMethod]
    public async Task SearchAsync_MalformedResponse_ThrowsWithClearMessage()
    {
        _handler.RespondWithBody("<html>Service unavailable</html>", HttpStatusCode.ServiceUnavailable);

        var exception = await AssertThrowsAsync<ExternalServiceException>(() => Client().SearchAsync("matrix"));

        Assert.AreEqual("OMDb returned an unexpected response.", exception.Message);
    }

    private IMovieCatalogClient Client(string? apiKey = "test-key")
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MovieManagerDatabase"] = "Server=unused;Database=unused;",
                ["Omdb:ApiKey"] = apiKey
            })
            .Build();

        services.AddInfrastructure(configuration);
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => _handler));

        return services.BuildServiceProvider().GetRequiredService<IMovieCatalogClient>();
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

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private Func<HttpResponseMessage>? _respond;
        private Exception? _failure;

        public int RequestCount { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        public void RespondWith(string fixture, HttpStatusCode status = HttpStatusCode.OK)
        {
            string body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture));
            RespondWithBody(body, status);
        }

        public void RespondWithBody(string body, HttpStatusCode status)
        {
            _respond = () => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        public void FailWith(Exception failure)
        {
            _failure = failure;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;
            if (_failure is not null)
            {
                throw _failure;
            }

            return Task.FromResult(_respond!());
        }
    }
}
