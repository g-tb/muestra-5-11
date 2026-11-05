// ReSharper disable UnusedMethodReturnValue.Global

using Application.Dtos;
using Domain.Entities;

namespace Application.Services;

public interface IActorService
{
    ActorDTO AddMovie(Actor movie);
    IList<ActorDTO> GetAll();
    ActorDTO FindById(int id);
    ActorDTO Update(ActorDTO updatedActor);
    void Delete(int id);
}
