using Application.Interfaces;
using Domain.Entities;
using Infrastructure.External;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("MovieManagerDatabase")
                                  ?? throw new InvalidOperationException(
                                      "Connection string 'MovieManagerDatabase' is not configured.");

        services.AddDbContext<MovieManagerDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IRepository<Movie>, MovieSqlRepository>();
        services.AddScoped<IRepository<Actor>, SqlRepository<Actor>>();
        services.AddScoped<IRepository<Category>, SqlRepository<Category>>();

        // The API key is read on each call, so a missing key is reported clearly instead of failing at startup.
        services.AddHttpClient(nameof(OmdbClient), client =>
            {
                client.BaseAddress = new Uri("https://www.omdbapi.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddTypedClient<IMovieCatalogClient>(httpClient => new OmdbClient(httpClient, configuration));

        return services;
    }
}
