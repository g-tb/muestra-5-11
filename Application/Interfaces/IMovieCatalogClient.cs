namespace Application.Interfaces;

/// <summary>
/// An external movie catalog (implemented with OMDb in Infrastructure). Implementations return
/// clean values: "no value" is <c>null</c> or an empty list, never a provider-specific marker.
/// Failures (missing or invalid key, rate limit, network) throw
/// <see cref="Application.Exceptions.ExternalServiceException"/>.
/// </summary>
public interface IMovieCatalogClient
{
    /// <summary>Searches movies by title. Returns an empty list when nothing matches.</summary>
    Task<IReadOnlyList<CatalogSearchItem>> SearchAsync(string title, CancellationToken cancellationToken = default);

    /// <summary>Gets a movie by its IMDb id. Returns <c>null</c> when the catalog doesn't know it.</summary>
    Task<CatalogMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
}

public record CatalogSearchItem(string ImdbId, string Title, int? Year, string? PosterUrl);

public record CatalogMovie(
    string ImdbId,
    string Title,
    int? Year,
    string? Director,
    decimal? Rating,
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Genres);
