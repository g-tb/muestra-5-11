using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

internal class ActorSqlRepository(MovieManagerDbContext dbContext) : IRepository<Actor>
{
    public Actor Add(Actor oneElement)
    {
        dbContext.Actors.Add(oneElement);
        dbContext.SaveChanges();
        return oneElement;
    }

    public Actor? Find(Func<Actor, bool> filter)
    {
        return dbContext.Actors.AsEnumerable().FirstOrDefault(filter);
    }

    public IList<Actor> FindAll()
    {
        return dbContext.Actors.ToList();
    }

    public Actor? Update(Actor updatedEntity)
    {
        Actor? foundActor = dbContext.Actors.Find(updatedEntity.Id);
        if (foundActor is null)
        {
            return null;
        }

        foundActor.Name = updatedEntity.Name;
        foundActor.Bio = updatedEntity.Bio;
        foundActor.BirthDate = updatedEntity.BirthDate;
        dbContext.SaveChanges();
        return foundActor;
    }

    public void Delete(int id)
    {
        Actor? foundActor = dbContext.Actors.Find(id);
        if (foundActor is null)
        {
            return;
        }

        dbContext.Actors.Remove(foundActor);
        dbContext.SaveChanges();
    }
}
