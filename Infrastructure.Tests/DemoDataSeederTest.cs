using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

[TestClass]
public class DemoDataSeederTest
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("MOVIEMANAGER_SQL_TEST_CONNECTION");

    [TestMethod]
    public async Task SeedDemoDataIfEnabledAsync_DoesNothing_WhenDisabled()
    {
        ServiceProvider provider = BuildProvider("Server=localhost;Database=Unused;", seedOnStartup: false);
        IConfiguration configuration = BuildConfiguration("Server=localhost;Database=Unused;", seedOnStartup: false);

        await provider.SeedDemoDataIfEnabledAsync(configuration);
    }

    [TestMethod]
    public async Task SeedDemoDataIfEnabledAsync_CreatesExpectedData_WhenSqlConnectionConfigured()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        using ServiceProvider provider = BuildProvider(ConnectionString, seedOnStartup: true);
        IConfiguration configuration = BuildConfiguration(ConnectionString, seedOnStartup: true);

        await provider.SeedDemoDataIfEnabledAsync(configuration);
        await provider.SeedDemoDataIfEnabledAsync(configuration);

        using IServiceScope scope = provider.CreateScope();
        IRepository<Category> categories = scope.ServiceProvider.GetRequiredService<IRepository<Category>>();
        IRepository<Actor> actors = scope.ServiceProvider.GetRequiredService<IRepository<Actor>>();
        IRepository<Movie> movies = scope.ServiceProvider.GetRequiredService<IRepository<Movie>>();

        Assert.AreEqual(1, categories.FindAll().Count(category => category.Name == "Action"));
        Assert.AreEqual(1, categories.FindAll().Count(category => category.Name == "Drama"));
        Assert.AreEqual(1, categories.FindAll().Count(category => category.Name == "Science Fiction"));
        Assert.AreEqual(1, actors.FindAll().Count(actor => actor.Name == "Keanu Reeves"));
        Assert.AreEqual(1, movies.FindAll().Count(movie => movie.Title == "The Matrix"));

        Movie matrix = movies.Find(movie => movie.Title == "The Matrix")!;
        Assert.AreEqual("Science Fiction", matrix.Category!.Name);
        Assert.IsTrue(matrix.Actors.Any(actor => actor.Name == "Keanu Reeves"));
        Assert.IsTrue(matrix.Actors.Any(actor => actor.Name == "Carrie-Anne Moss"));
    }

    private static ServiceProvider BuildProvider(string connectionString, bool seedOnStartup)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfiguration(connectionString, seedOnStartup));
        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration(string connectionString, bool seedOnStartup)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MovieManagerDatabase"] = connectionString,
                ["DemoData:SeedOnStartup"] = seedOnStartup.ToString()
            })
            .Build();
    }
}
