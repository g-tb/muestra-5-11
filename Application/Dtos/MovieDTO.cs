// ReSharper disable InconsistentNaming

using Domain.Entities;

namespace Application.Dtos;

public class MovieDTO
{
    public int Id { get; private init; }

    public string? Title { get; set; }

    public string? Director { get; set; }

    public int? ReleaseYear { get; set; }
    
    public CategoryDTO? Category { get; set; }
    
    public List<ActorDTO> Actors { get; set; } = new();
    
    public bool HasActor(int id){
        return Actors.Any(a => a.Id == id);
    }

    public Movie toEntity()
    {
        return new Movie()
        {
            Id = this.Id,
            Title = this.Title,
            Director = this.Director,
            ReleaseYear = this.ReleaseYear,
            Actors = this.Actors.Select(x => x.toEntity()).ToList(),
            Category = this.Category?.toEntity()
        };
    }
    
    public static MovieDTO fromEntity(Movie movie)
    {
        return new MovieDTO()
        {
            Id = movie.Id,
            Title = movie.Title,
            Director = movie.Director,
            ReleaseYear = movie.ReleaseYear,
            Category = CategoryDTO.fromEntity(movie.Category),
            Actors = movie.Actors.Select(x => ActorDTO.fromEntity(x)!).ToList()
        };
    }
}
