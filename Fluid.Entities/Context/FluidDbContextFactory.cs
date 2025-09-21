using Fluid.Entities.IAM;
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
            connectionString = "Host=dev2.outamationlabs.com;Port=5555;Database=FluidDb;Username=postgres;Password=KeshavDB@1933;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<FluidDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = "design-time",
            Name = "Design Time Tenant",
            ConnectionString = connectionString
        };
        return new FluidDbContext(optionsBuilder.Options, tenant);
    }
}