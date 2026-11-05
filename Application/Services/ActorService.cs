using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

internal class ActorService(IRepository<Actor> actorRepository) : IActorService
{
    public ActorDTO AddMovie(Actor movie)
    {
        return ActorDTO.fromEntity(actorRepository.Add(movie))!;
    }

    public IList<ActorDTO> GetAll()
    {
        return actorRepository.FindAll().Select(x => ActorDTO.fromEntity(x)!).ToList();
    }

    public ActorDTO FindById(int id)
    {
        return ActorDTO.fromEntity(actorRepository.Find(x => x.Id == id))!;
    }

    public ActorDTO Update(ActorDTO updatedActor)
    {
        return ActorDTO.fromEntity(actorRepository.Update(updatedActor.toEntity()))!;
    }

    public void Delete(int id)
    {
        actorRepository.Delete(id);
    }
}
