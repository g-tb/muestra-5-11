using Domain.Entities;

namespace Application.Interfaces;

public interface IRepository<TEntity>
    where TEntity : IEntity
{
    TEntity Add(TEntity oneElement);
    TEntity? Find(Func<TEntity, bool> filter);
    IList<TEntity> FindAll();
    TEntity? Update(TEntity updatedEntity);
    void Delete(int id);
}
