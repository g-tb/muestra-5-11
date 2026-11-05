using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DemoDataSeedingExtensions
{
    public static async Task SeedDemoDataIfEnabledAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(configuration["DemoData:SeedOnStartup"], out bool seedOnStartup) || !seedOnStartup)
        {
            return;
        }

        using IServiceScope scope = serviceProvider.CreateScope();
        DemoDataSeeder seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
