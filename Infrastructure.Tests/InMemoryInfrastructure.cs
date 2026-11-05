using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

/// <summary>
/// Builds the real Infrastructure registrations (<see cref="DependencyInjection.AddInfrastructure"/>)
/// and swaps the SQL Server provider for the EF Core InMemory provider.
/// The DbContext type is internal, so it is discovered from the registered services instead of named.
/// </summary>
internal static class InMemoryInfrastructure
{
    public static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MovieManagerDatabase"] = "Server=unused;Database=unused;"
            })
            .Build();

        services.AddInfrastructure(configuration);

        Type contextType = services
            .Select(descriptor => descriptor.ServiceType)
            .Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
            .GetGenericArguments()[0];

        RemoveProviderConfiguration(services, contextType);

        MethodInfo configureDbContext = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(EntityFrameworkServiceCollectionExtensions.ConfigureDbContext)
                              && method.GetGenericArguments().Length == 1
                              && method.GetParameters().Length == 3
                              && method.GetParameters()[1].ParameterType == typeof(Action<DbContextOptionsBuilder>))
            .MakeGenericMethod(contextType);

        Action<DbContextOptionsBuilder> useInMemory = options => options.UseInMemoryDatabase(databaseName);
        configureDbContext.Invoke(null, [services, useInMemory, ServiceLifetime.Scoped]);

        return services.BuildServiceProvider();
    }

    private static void RemoveProviderConfiguration(IServiceCollection services, Type contextType)
    {
        Type configurationType = typeof(IDbContextOptionsConfiguration<>).MakeGenericType(contextType);
        foreach (ServiceDescriptor descriptor in services.Where(d => d.ServiceType == configurationType).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
