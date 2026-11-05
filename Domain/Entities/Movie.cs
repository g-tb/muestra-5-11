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

    public void Update(Movie updatedMovie)
    {
        Title = updatedMovie.Title;
        Director = updatedMovie.Director;
        ReleaseYear = updatedMovie.ReleaseYear;
        Category = updatedMovie.Category;
        Actors = updatedMovie.Actors;
    }
}
