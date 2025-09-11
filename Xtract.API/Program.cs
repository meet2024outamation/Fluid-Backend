using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Infrastructure.Services;
using Xtract.Entities.Context;
using Xtract.Entities.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Register application services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddDistributedMemoryCache();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<XtractDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "PostgreSQL connection string 'DefaultConnection' not found. " +
            "Please check your appsettings.json file.");
    }

    options.UseNpgsql(connectionString);

    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Xtract API",
        Version = "v1",
        Description = "API for managing document extraction, processing clients, schemas, and batch operations",
    });

    // Enable annotations for better Swagger documentation
    c.EnableAnnotations();

    // Include XML comments if you have them
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // if (File.Exists(xmlPath))
    //     c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Xtract API V1");
        c.RoutePrefix = string.Empty; // This makes Swagger UI available at the root URL
        c.DocumentTitle = "Xtract API Documentation";
        c.DisplayRequestDuration();
    });

    // Database initialization in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<XtractDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("🔌 Initializing PostgreSQL database...");

            // This will create the database if it doesn't exist
            // Note: The postgres user must have CREATEDB privileges
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("✅ Database created/verified successfully!");

            // Apply any pending migrations (in case you add them later)
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("🔄 Applying pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied successfully!");
            }
            else
            {
                logger.LogInformation("✅ Database is up to date!");
            }

            // Seed initial data
            await SeedDatabaseAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database initialization failed: {Message}", ex.Message);
            logger.LogError("🔧 Please check your PostgreSQL connection and ensure:");
            logger.LogError("   1. PostgreSQL is running");
            logger.LogError("   2. Connection string is correct in appsettings.json");
            logger.LogError("   3. PostgreSQL user has CREATEDB privileges");
            logger.LogError("   4. User has proper permissions");

            // Try manual database creation guidance
            logger.LogWarning("💡 Alternative: Create database manually in pgAdmin:");
            logger.LogWarning("   1. Open pgAdmin");
            logger.LogWarning("   2. Right-click server → Create → Database");
            logger.LogWarning("   3. Name: 'XtractDb' (or as per your connection string)");
            logger.LogWarning("   4. Restart the application");

            throw;
        }
    }
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task SeedDatabaseAsync(XtractDbContext context, ILogger logger)
{
    try
    {
        // Seed default roles if none exist
        if (!await context.Roles.AnyAsync())
        {
            logger.LogInformation("Seeding default roles...");

            var roles = new[]
            {
                new Role
                {
                    Name = "Admin",
                    IsEditable = false,
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Name = "Manager",
                    IsEditable = true,
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Name = "Operator",
                    IsEditable = true,
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Default roles seeded successfully!");
        }

        // Seed default users if none exist
        if (!await context.Users.AnyAsync())
        {
            logger.LogInformation("Seeding default users...");

            var users = new[]
            {
                new User
                {
                    AzureAdId = "system-default",
                    Email = "system@xtract.com",
                    FirstName = "System",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    AzureAdId = "admin-user",
                    Email = "admin@xtract.com",
                    FirstName = "Admin",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Assign admin role to admin user
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.AzureAdId == "admin-user");

            if (adminRole != null && adminUser != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    CreatedDateTime = DateTimeOffset.UtcNow
                };

                context.UserRoles.Add(userRole);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("✅ Default users seeded successfully!");
        }

        // Seed default permissions if none exist
        if (!await context.Permissions.AnyAsync())
        {
            logger.LogInformation("Seeding default permissions...");

            var permissions = new[]
            {
                new Permission
                {
                    Name = "View Administration",
                    Code = "VIEW_ADMIN",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "Edit Administration",
                    Code = "EDIT_ADMIN",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "View Configuration",
                    Code = "VIEW_CONFIG",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "Edit Configuration",
                    Code = "EDIT_CONFIG",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "View Operations",
                    Code = "VIEW_OPS",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "Edit Operations",
                    Code = "EDIT_OPS",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "View Reports",
                    Code = "VIEW_REPORTS",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Permission
                {
                    Name = "Edit Reports",
                    Code = "EDIT_REPORTS",
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                }
            };

            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Default permissions seeded successfully!");
        }

        logger.LogInformation("✅ Database seeding completed!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding database");
        throw;
    }
}
