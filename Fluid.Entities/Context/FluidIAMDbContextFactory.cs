using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Fluid.Entities.Context;

public class FluidIAMDbContextFactory : IDesignTimeDbContextFactory<FluidIAMDbContext>
{
    public FluidIAMDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<FluidIAMDbContextFactory>()
            .Build();

        var connectionString = configuration.GetConnectionString("IAMConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback connection string for design-time if not found in config
            connectionString = "Host=localhost;Database=fluid_iam;Username=postgres;Password=your_password";
        }

        var optionsBuilder = new DbContextOptionsBuilder<FluidIAMDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FluidIAMDbContext(optionsBuilder.Options);
    }
}