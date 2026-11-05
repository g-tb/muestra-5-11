using Application.Interfaces;
using Domain.Entities;
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
        services.AddScoped<IRepository<Actor>, ActorSqlRepository>();
        services.AddScoped<IRepository<Category>, CategorySqlRepository>();

        return services;
    }
}
