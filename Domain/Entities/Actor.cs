// ReSharper disable UnusedMember.Global

namespace Domain.Entities;

public class Actor : IEntity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Bio { get; set; }

    public DateTime? BirthDate { get; set; }

    public void Update(Actor updatedActor)
    {
        Id = updatedActor.Id;
        Name = updatedActor.Name;
        Bio = updatedActor.Bio;
        BirthDate = updatedActor.BirthDate;
    }
}
