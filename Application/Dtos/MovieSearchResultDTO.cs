// ReSharper disable InconsistentNaming

using Application.Interfaces;

namespace Application.Dtos;

public class MovieSearchResultDTO
{
    public string ImdbId { get; private init; } = "";

    public string Title { get; private init; } = "";

    public int? Year { get; private init; }

    public string? PosterUrl { get; private init; }

    public static MovieSearchResultDTO fromCatalog(CatalogSearchItem item)
    {
        return new MovieSearchResultDTO()
        {
            ImdbId = item.ImdbId,
            Title = item.Title,
            Year = item.Year,
            PosterUrl = item.PosterUrl
        };
    }
}
