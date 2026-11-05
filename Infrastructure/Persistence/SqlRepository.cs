using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Template Method: the public CRUD methods hold the algorithm; derived repositories
/// customize it by overriding the protected steps. Simple entities use this class directly.
/// </summary>
internal class SqlRepository<TEntity>(MovieManagerDbContext dbContext) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    protected MovieManagerDbContext DbContext => dbContext;

    protected DbSet<TEntity> Set => dbContext.Set<TEntity>();

    public TEntity Add(TEntity oneElement)
    {
        PrepareForAdd(oneElement);
        Set.Add(oneElement);
        dbContext.SaveChanges();
        return oneElement;
    }

    public TEntity? Find(Expression<Func<TEntity, bool>> filter)
    {
        return Query().FirstOrDefault(filter);
    }

    public IList<TEntity> FindAll()
    {
        return Query().ToList();
    }

    public TEntity? Update(TEntity updatedEntity)
    {
        TEntity? foundEntity = FindTracked(updatedEntity.Id);
        if (foundEntity is null)
        {
            return null;
        }

        ApplyChanges(foundEntity, updatedEntity);
        dbContext.SaveChanges();
        return foundEntity;
    }

    public void Delete(int id)
    {
        TEntity? foundEntity = FindTracked(id);
        if (foundEntity is null)
        {
            return;
        }

        Set.Remove(foundEntity);
        dbContext.SaveChanges();
    }

    /// <summary>Query used by Find and FindAll; override to include relationships.</summary>
    protected virtual IQueryable<TEntity> Query()
    {
        return Set;
    }

    /// <summary>Loads the tracked entity that Update and Delete operate on.</summary>
    protected virtual TEntity? FindTracked(int id)
    {
        return Set.Find(id);
    }

    /// <summary>Runs before a new entity is added; override to attach relationships.</summary>
    protected virtual void PrepareForAdd(TEntity entity)
    {
    }

    /// <summary>Copies the updated values onto the tracked entity. The default copies mapped scalar properties.</summary>
    protected virtual void ApplyChanges(TEntity target, TEntity source)
    {
        dbContext.Entry(target).CurrentValues.SetValues(source);
    }
}
