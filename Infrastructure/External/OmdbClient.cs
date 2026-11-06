using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.External;

/// <summary>
/// <see cref="IMovieCatalogClient"/> backed by the OMDb API (https://www.omdbapi.com/).
/// Every OMDb detail stays here: query parameters, JSON shape, "N/A" values, list and year
/// formats, and the error bodies OMDb returns.
/// </summary>
internal class OmdbClient(HttpClient httpClient, IConfiguration configuration) : IMovieCatalogClient
{
    public const string ApiKeySetting = "Omdb:ApiKey";

    private const string NotAvailable = "N/A";

    public async Task<IReadOnlyList<CatalogSearchItem>> SearchAsync(string title, CancellationToken cancellationToken = default)
    {
        OmdbResponse response = await GetAsync($"s={Uri.EscapeDataString(title)}&type=movie", cancellationToken);
        if (response.IsNotFound)
        {
            return [];
        }

        return (response.Search ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.ImdbId) && !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new CatalogSearchItem(item.ImdbId!, item.Title!, ParseYear(item.Year), ValueOrNull(item.Poster)))
            .ToList();
    }

    public async Task<CatalogMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        OmdbResponse response = await GetAsync($"i={Uri.EscapeDataString(imdbId)}", cancellationToken);
        if (response.IsNotFound || string.IsNullOrWhiteSpace(response.ImdbId) || string.IsNullOrWhiteSpace(response.Title))
        {
            return null;
        }

        return new CatalogMovie(
            response.ImdbId,
            response.Title,
            ParseYear(response.Year),
            ValueOrNull(response.Director),
            ParseRating(response.ImdbRating),
            ParseList(response.Actors),
            ParseList(response.Genre));
    }

    private async Task<OmdbResponse> GetAsync(string query, CancellationToken cancellationToken)
    {
        string? apiKey = configuration[ApiKeySetting];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ExternalServiceException($"The OMDb API key is not configured ({ApiKeySetting}).");
        }

        string body;
        try
        {
            using HttpResponseMessage httpResponse = await httpClient.GetAsync(
                $"?apikey={Uri.EscapeDataString(apiKey.Trim())}&{query}", cancellationToken);
            body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalServiceException("Could not reach OMDb. Check your connection.", exception);
        }

        OmdbResponse response = Deserialize(body);
        if (response.Response == "False" && !response.IsNotFound)
        {
            throw new ExternalServiceException(ErrorMessage(response.Error));
        }

        return response;
    }

    private static OmdbResponse Deserialize(string body)
    {
        try
        {
            OmdbResponse? response = JsonSerializer.Deserialize<OmdbResponse>(body);
            if (response?.Response is "True" or "False")
            {
                return response;
            }
        }
        catch (JsonException)
        {
        }

        throw new ExternalServiceException("OMDb returned an unexpected response.");
    }

    private static string ErrorMessage(string? error)
    {
        return error switch
        {
            "Invalid API key!" => "The OMDb API key is invalid.",
            "Request limit reached!" => "The OMDb daily request limit has been reached. Try again tomorrow.",
            "Too many results." => "Too many results; type a more specific title.",
            "No API key provided." => $"The OMDb API key is not configured ({ApiKeySetting}).",
            _ => $"OMDb returned an error: {error ?? "unknown"}."
        };
    }

    private static string? ValueOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim() == NotAvailable ? null : value.Trim();
    }

    // OMDb years look like "1999", "2019–2022" or "2019–": the first four digits are the release year.
    private static int? ParseYear(string? value)
    {
        string? year = ValueOrNull(value);
        return year is { Length: >= 4 } && int.TryParse(year.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseRating(string? value)
    {
        string? rating = ValueOrNull(value);
        return rating is not null && decimal.TryParse(rating, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : null;
    }

    private static IReadOnlyList<string> ParseList(string? value)
    {
        string? list = ValueOrNull(value);
        return list is null
            ? []
            : list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(item => item != NotAvailable)
                .ToList();
    }

    private sealed record OmdbResponse
    {
        public string? Response { get; init; }

        public string? Error { get; init; }

        public List<OmdbSearchItem>? Search { get; init; }

        public string? Title { get; init; }

        public string? Year { get; init; }

        public string? Director { get; init; }

        public string? Actors { get; init; }

        public string? Genre { get; init; }

        [JsonPropertyName("imdbRating")]
        public string? ImdbRating { get; init; }

        [JsonPropertyName("imdbID")]
        public string? ImdbId { get; init; }

        [JsonIgnore]
        public bool IsNotFound => Response == "False" && Error is "Movie not found!" or "Incorrect IMDb ID.";
    }

    private sealed record OmdbSearchItem
    {
        public string? Title { get; init; }

        public string? Year { get; init; }

        [JsonPropertyName("imdbID")]
        public string? ImdbId { get; init; }

        public string? Poster { get; init; }
    }
}
