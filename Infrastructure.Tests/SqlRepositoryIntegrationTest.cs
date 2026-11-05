using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

[TestClass]
public class SqlRepositoryIntegrationTest
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("MOVIEMANAGER_SQL_TEST_CONNECTION");

    [TestMethod]
    public void MovieRepository_RoundTripsRelations_WhenSqlConnectionConfigured()
    {
        if (!IsSqlTestEnabled())
        {
            return;
        }

        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        IRepository<Movie> movies = scope.ServiceProvider.GetRequiredService<IRepository<Movie>>();
        IRepository<Actor> actors = scope.ServiceProvider.GetRequiredService<IRepository<Actor>>();
        IRepository<Category> categories = scope.ServiceProvider.GetRequiredService<IRepository<Category>>();

        string suffix = Guid.NewGuid().ToString("N");
        Category category = categories.Add(new Category { Name = $"Category {suffix}" });
        Actor actor = actors.Add(new Actor { Name = $"Actor {suffix}", Bio = "Bio", BirthDate = DateTime.UtcNow.Date });
        Movie movie = movies.Add(new Movie
        {
            Title = $"Movie {suffix}",
            Director = "Director",
            ReleaseYear = 2026,
            Category = category,
            Actors = [actor]
        });

        Movie found = movies.Find(x => x.Id == movie.Id)!;

        Assert.AreEqual(movie.Title, found.Title);
        Assert.AreEqual(category.Id, found.Category!.Id);
        Assert.IsTrue(found.HasActor(actor.Id));

        found.Title = $"Movie updated {suffix}";
        found.Category = null;
        found.Actors = [];
        Movie updated = movies.Update(found)!;

        Assert.AreEqual(found.Title, updated.Title);
        Assert.IsNull(updated.Category);
        Assert.AreEqual(0, updated.Actors.Count);

        movies.Delete(movie.Id);
        actors.Delete(actor.Id);
        categories.Delete(category.Id);
    }

    [TestMethod]
    public void ActorRepository_UpdatesAndDeletes_WhenSqlConnectionConfigured()
    {
        if (!IsSqlTestEnabled())
        {
            return;
        }

        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        IRepository<Actor> actors = scope.ServiceProvider.GetRequiredService<IRepository<Actor>>();

        string suffix = Guid.NewGuid().ToString("N");
        Actor actor = actors.Add(new Actor { Name = $"Actor {suffix}", Bio = "Original" });

        actor.Bio = "Updated";
        Actor updated = actors.Update(actor)!;

        Assert.AreEqual("Updated", updated.Bio);

        actors.Delete(actor.Id);
        Assert.IsNull(actors.Find(x => x.Id == actor.Id));
    }

    [TestMethod]
    public void CategoryRepository_UpdatesAndDeletes_WhenSqlConnectionConfigured()
    {
        if (!IsSqlTestEnabled())
        {
            return;
        }

        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        IRepository<Category> categories = scope.ServiceProvider.GetRequiredService<IRepository<Category>>();

        string suffix = Guid.NewGuid().ToString("N");
        Category category = categories.Add(new Category { Name = $"Category {suffix}" });

        category.Name = $"Updated {suffix}";
        Category updated = categories.Update(category)!;

        Assert.AreEqual(category.Name, updated.Name);

        categories.Delete(category.Id);
        Assert.IsNull(categories.Find(x => x.Id == category.Id));
    }

    private static bool IsSqlTestEnabled()
    {
        return !string.IsNullOrWhiteSpace(ConnectionString);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MovieManagerDatabase"] = ConnectionString
            })
            .Build();

        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
