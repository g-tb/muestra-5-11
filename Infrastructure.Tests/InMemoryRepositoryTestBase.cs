using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

/// <summary>
/// Gives each test a fresh InMemory database. <see cref="Repository"/> lives in one scope (one DbContext);
/// <see cref="FreshRepository"/> opens a new scope over the same store to check what was actually persisted.
/// </summary>
public abstract class InMemoryRepositoryTestBase<TEntity>
    where TEntity : IEntity
{
    private ServiceProvider _provider = null!;
    private readonly List<IServiceScope> _scopes = [];

    protected IRepository<TEntity> Repository { get; private set; } = null!;

    [TestInitialize]
    public void SetUpProvider()
    {
        _provider = InMemoryInfrastructure.BuildProvider(Guid.NewGuid().ToString());
        Repository = RepositoryInNewScope<TEntity>();
    }

    [TestCleanup]
    public void TearDownProvider()
    {
        foreach (IServiceScope scope in _scopes)
        {
            scope.Dispose();
        }
        _scopes.Clear();
        _provider.Dispose();
    }

    protected IRepository<TEntity> FreshRepository()
    {
        return RepositoryInNewScope<TEntity>();
    }

    protected IRepository<T> RepositoryInNewScope<T>()
        where T : IEntity
    {
        IServiceScope scope = _provider.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<IRepository<T>>();
    }
}
