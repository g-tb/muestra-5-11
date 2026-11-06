using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests;

/// <summary>In-memory <see cref="IRepository{TEntity}"/> test double that assigns ids on add.</summary>
internal class FakeRepository<TEntity> : IRepository<TEntity>
    where TEntity : IEntity
{
    private readonly List<TEntity> _items = new();

    public TEntity Add(TEntity oneElement)
    {
        oneElement.Id = _items.Select(x => x.Id).DefaultIfEmpty().Max() + 1;
        _items.Add(oneElement);
        return oneElement;
    }

    public TEntity? Find(Expression<Func<TEntity, bool>> filter)
    {
        return _items.AsQueryable().Where(filter).FirstOrDefault();
    }

    public IList<TEntity> FindAll()
    {
        return _items.ToList();
    }

    public TEntity? Update(TEntity updatedEntity)
    {
        throw new NotSupportedException("Not needed by the tests that use this fake.");
    }

    public void Delete(int id)
    {
        _items.RemoveAll(x => x.Id == id);
    }
}
