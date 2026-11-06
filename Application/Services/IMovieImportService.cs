using Application.Dtos;

namespace Application.Services;

/// <summary>Searches an external movie catalog and imports movies from it.</summary>
public interface IMovieImportService
{
    /// <summary>Returns the first results for <paramref name="query"/>; an empty list when nothing matches.</summary>
    Task<IList<MovieSearchResultDTO>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    /// <summary>Imports the movie with that IMDb id, reusing actors and categories that already exist.</summary>
    Task<MovieDTO> ImportAsync(string imdbId, CancellationToken cancellationToken = default);
}
