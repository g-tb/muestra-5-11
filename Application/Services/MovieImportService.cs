using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

internal class MovieImportService(
    IMovieCatalogClient catalog,
    IRepository<Movie> moviesRepository,
    IRepository<Actor> actorsRepository,
    IRepository<Category> categoriesRepository) : IMovieImportService
{
    private const int MaxSearchResults = 5;

    public async Task<IList<MovieSearchResultDTO>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new LogicException("Enter a title to search.");
        }

        IReadOnlyList<CatalogSearchItem> results = await catalog.SearchAsync(query.Trim(), cancellationToken);
        return results.Take(MaxSearchResults).Select(MovieSearchResultDTO.fromCatalog).ToList();
    }

    public async Task<MovieDTO> ImportAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
        {
            throw new LogicException("Choose a movie to import.");
        }

        // Checked before calling the catalog, so importing twice never reaches OMDb.
        string normalizedImdbId = imdbId.Trim().ToLowerInvariant();
        if (moviesRepository.Find(movie => movie.ImdbId == normalizedImdbId) is not null)
        {
            throw new LogicException("This movie has already been imported.");
        }

        CatalogMovie details = await catalog.GetByImdbIdAsync(normalizedImdbId, cancellationToken)
                               ?? throw new LogicException("OMDb has no movie with that id.");

        // Checked before creating actors or categories, so a rejected import leaves nothing behind.
        string titleKey = NameKey(details.Title);
        if (moviesRepository.Find(movie => movie.Title != null && movie.Title.Trim().ToLower() == titleKey) is not null)
        {
            throw new LogicException($"A movie titled '{details.Title.Trim()}' already exists.");
        }

        var movie = new Movie
        {
            Title = details.Title.Trim(),
            Director = details.Director,
            ReleaseYear = details.Year,
            ImdbId = details.ImdbId,
            ImdbRating = details.Rating,
            Category = details.Genres.Count > 0 ? FindOrCreateCategory(details.Genres[0]) : null,
            Actors = details.Actors
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .DistinctBy(NameKey)
                .Select(FindOrCreateActor)
                .ToList()
        };

        return MovieDTO.fromEntity(moviesRepository.Add(movie));
    }

    private Category FindOrCreateCategory(string name)
    {
        string key = NameKey(name);
        return categoriesRepository.Find(category => category.Name != null && category.Name.Trim().ToLower() == key)
               ?? categoriesRepository.Add(new Category { Name = name.Trim() });
    }

    private Actor FindOrCreateActor(string name)
    {
        string key = NameKey(name);
        return actorsRepository.Find(actor => actor.Name != null && actor.Name.Trim().ToLower() == key)
               ?? actorsRepository.Add(new Actor { Name = name.Trim() });
    }

    /// <summary>
    /// Names (and titles) match when they are equal ignoring case and surrounding spaces. The rule is
    /// stated here, not left to the database collation; the lookups still run as SQL filters.
    /// </summary>
    private static string NameKey(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
