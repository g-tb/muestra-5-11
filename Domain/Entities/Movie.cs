// ReSharper disable UnusedMember.Global

using Domain.Exceptions;

namespace Domain.Entities;

public class Movie : IEntity
{
    public int Id { get; set; }

    public string? Title
    {
        get;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new DomainException("Title must not be empty");
            }
            field = value;
        }
    }

    public string? Director { get; set; }

    public int? ReleaseYear { get; set; }

    public Category? Category { get; set; }

    public List<Actor> Actors { get; set; } = new();

    public bool HasActor(int id)
    {
        return Actors.Any(a => a.Id == id);
    }

    public double? Budget
    {
        get;
        set
        {
            if (value is null || value < 0)
            {
                throw new DomainException("Budget must not be empty or negative");
            }
            field = value;
        }
    }

    /// <summary>Optional IMDb id, stored trimmed and lowercased so comparisons don't depend on collation.</summary>
    public string? ImdbId
    {
        get;
        set
        {
            if (value is not null && string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException("IMDb id must not be empty");
            }
            field = value?.Trim().ToLowerInvariant();
        }
    }

    public decimal? ImdbRating
    {
        get;
        set
        {
            if (value is < 0m or > 10m)
            {
                throw new DomainException("IMDb rating must be between 0 and 10");
            }
            field = value;
        }
    }

    public void Update(Movie updatedMovie)
    {
        Title = updatedMovie.Title;
        Director = updatedMovie.Director;
        ReleaseYear = updatedMovie.ReleaseYear;
        Category = updatedMovie.Category;
        Actors = updatedMovie.Actors;
        ImdbId = updatedMovie.ImdbId;
        ImdbRating = updatedMovie.ImdbRating;
    }
}
