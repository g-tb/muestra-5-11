// ReSharper disable InconsistentNaming

using Domain.Entities;

namespace Application.Dtos;

public class ActorDTO
{
    public int Id { get; private init; }

    public string? Name { get; set; }

    public string? Bio { get; set; }

    public DateTime? BirthDate { get; set; }
    
    public Actor toEntity()
    {
        return new Actor()
        {
            Id = this.Id,
            Name = this.Name,
            Bio = this.Bio,
            BirthDate = this.BirthDate
        };
    }
    
    public static ActorDTO? fromEntity(Actor? actor)
    {
        if (actor is null)
        {
            return null;
        }
        return new ActorDTO()
        {
            Id = actor.Id,
            Name = actor.Name,
            Bio = actor.Bio,
            BirthDate = actor.BirthDate
        };
    }
}
