using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Fluid.Entities.Context;

public class FluidDbContextFactory : IDesignTimeDbContextFactory<FluidDbContext>
{
    public FluidDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<FluidDbContextFactory>()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback connection string for design-time if not found in config
            connectionString = "Host=localhost;Database=fluid_default;Username=postgres;Password=your_password";
        }

        var optionsBuilder = new DbContextOptionsBuilder<FluidDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FluidDbContext(optionsBuilder.Options);
    }
}