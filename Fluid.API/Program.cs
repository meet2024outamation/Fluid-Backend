using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Fluid.API.Authorization;
using Fluid.API.Constants;
using Fluid.API.Helpers;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Infrastructure.Services;
using Fluid.Entities.Context;
using Fluid.Entities.IAM;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using SharedKernel.Models;
using SharedKernel.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdConfig"));

builder.Services.AddAuthorization(options =>
{
    // Define policies for different roles
    options.AddPolicy(AuthorizationPolicies.ProductOwnerPolicy, policy =>
        policy.Requirements.Add(new RoleRequirement(ApplicationRoles.ProductOwner)));

    options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
        policy.Requirements.Add(new RoleRequirement(ApplicationRoles.TenantAdmin)));

    options.AddPolicy(AuthorizationPolicies.ManagerPolicy, policy =>
        policy.Requirements.Add(new RoleRequirement(ApplicationRoles.Manager, ApplicationRoles.TenantAdmin)));

    options.AddPolicy(AuthorizationPolicies.OperatorPolicy, policy =>
        policy.Requirements.Add(new RoleRequirement(ApplicationRoles.Operator, ApplicationRoles.Manager, ApplicationRoles.TenantAdmin)));
});

// Register authorization handler
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

// Configure Azure AD settings
builder.Services.Configure<AzureADConfig>(builder.Configuration.GetSection("AzureAdConfig"));

// Register application services
builder.Services.AddTransient<IProjectService, ProjectService>();
builder.Services.AddTransient<ICurrentUserService, CurrentUserService>();
builder.Services.AddTransient<IGraphService, GraphService>();
builder.Services.AddTransient<IManageUserService, ManageUserService>();
builder.Services.AddTransient<IBatchService, BatchService>();
builder.Services.AddTransient<IOrderService, OrderService>();
// TODO: Uncomment when SimpleFieldMappingService is needed
builder.Services.AddTransient<IFieldMappingService, FieldMappingService>();
builder.Services.AddTransient<ISchemaService, SchemaService>();
builder.Services.AddTransient<IGlobalSchemaService, GlobalSchemaService>();
builder.Services.AddTransient<IUser, AuthUser>();
builder.Services.AddTransient<ITenantService, TenantService>();

builder.Services.AddMultiTenant<Tenant>()
    .WithHeaderStrategy("X-Tenant-Id")
    .WithEFCoreStore<FluidIAMDbContext, Tenant>();

// Add CORS

builder.Services.AddDbContext<FluidDbContext>((serviceProvider, options) =>
{
    var tenantInfo = serviceProvider.GetService<ITenantInfo>() as Tenant;

    var connectionString = tenantInfo?.ConnectionString ??
                           builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("No tenant connection string found!");

    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention();

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});
builder.Services.AddDbContext<FluidIAMDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("IAMConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "PostgreSQL connection string 'IAMConnection' not found. " +
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fluid.API",
        Version = "v1",
        Description = "API for managing document extraction, processing projects, schemas, and batch operations",
        Contact = new OpenApiContact
        {
            Name = "Fluid API Support",
            Email = "support@fluid.com"
        }
    });

    // Enable annotations for better Swagger documentation
    c.EnableAnnotations();

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fluid.API V1");
        c.RoutePrefix = string.Empty; // This makes Swagger UI available at the root URL
        c.DocumentTitle = "Fluid.API Documentation";
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelExpandDepth(2);
        c.DefaultModelsExpandDepth(1);
    });

    // Database initialization in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<FluidDbContext>();
        var iamContext = scope.ServiceProvider.GetRequiredService<FluidIAMDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("🔌 Initializing PostgreSQL database...");

            // Initialize IAM database first (this contains tenant information)
            await iamContext.Database.EnsureCreatedAsync();

            var pendingIAMMigrations = await iamContext.Database.GetPendingMigrationsAsync();
            if (pendingIAMMigrations.Any())
            {
                logger.LogInformation("🔄 Applying pending IAM migrations...");
                await iamContext.Database.MigrateAsync();
                logger.LogInformation("✅ IAM migrations applied successfully!");
            }
            else
            {
                logger.LogInformation("✅ IAM database is up to date!");
            }

            // Seed initial data first (this may create tenants)
            //await SeedDatabaseAsync(context, iamContext, logger, builder.Configuration);

            // Apply migrations to all tenant databases
            //logger.LogInformation("🏢 Starting multi-tenant migration process...");
            await MigrationHelper.ApplyMigrationsAsync(app.Services, logger);

            logger.LogInformation("✅ Database initialization completed successfully!");
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

// Add a root endpoint that redirects to Swagger in development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseMultiTenant();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log the Swagger URL on startup
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🚀 Application started!");
        logger.LogInformation("📖 Swagger UI available at: http://localhost:5086");
        logger.LogInformation("📖 Swagger UI available at: https://localhost:7253");
        logger.LogInformation("🌐 API Base URL: http://localhost:5086");
    });
}

app.Run();

static async Task SeedDatabaseAsync(FluidDbContext context, FluidIAMDbContext iamContext, ILogger logger, IConfiguration configuration)
{
    try
    {
        // Seed default roles if none exist
        if (!await iamContext.Roles.AnyAsync())
        {
            logger.LogInformation("Seeding default roles...");

            var roles = new[]
            {
                new Role
                {
                    Name = ApplicationRoles.ProductOwner,
                    Description = "Product Owner with full tenant management access",
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Name = ApplicationRoles.TenantAdmin,
                    Description = "Administrator with tenant access",
                    IsForServicePrincipal = false,
                    IsActive = true,
                    CreatedDateTime = DateTimeOffset.UtcNow
                }
            };

            iamContext.Roles.AddRange(roles);
            await iamContext.SaveChangesAsync();

            logger.LogInformation("✅ Default roles seeded successfully!");
        }

        // Seed default schemas if none exist
        if (!await context.Schemas.AnyAsync())
        {
            logger.LogInformation("Seeding default schemas...");

            var systemUser = await iamContext.Users.FirstOrDefaultAsync(u => u.AzureAdId == "system-default");
            if (systemUser != null)
            {
                var defaultSchema = new Fluid.Entities.Entities.Schema
                {
                    Name = "Default Loan Schema",
                    Description = "Default schema for loan processing",
                    Version = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = systemUser.Id
                };

                context.Schemas.Add(defaultSchema);
                await context.SaveChangesAsync();

                // Add default schema fields
                var schemaFields = new[]
                {
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "loan_id",
                        FieldLabel = "Loan ID",
                        DataType = SchemaFieldDataTypes.String,
                        IsRequired = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "borrower_name",
                        FieldLabel = "Borrower Name",
                        DataType = SchemaFieldDataTypes.String,
                        IsRequired = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "loan_amount",
                        FieldLabel = "Loan Amount",
                        DataType = SchemaFieldDataTypes.DateTime,
                        Format = "currency",
                        IsRequired = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "property_address",
                        FieldLabel = "Property Address",
                        DataType = SchemaFieldDataTypes.String,
                        IsRequired = true,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "application_date",
                        FieldLabel = "Application Date",
                        DataType = SchemaFieldDataTypes.Date,
                        Format = "MM/dd/yyyy",
                        IsRequired = false,
                        DisplayOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Fluid.Entities.Entities.SchemaField
                    {
                        SchemaId = defaultSchema.Id,
                        FieldName = "document_type",
                        FieldLabel = "Document Type",
                        DataType = SchemaFieldDataTypes.String,
                        IsRequired = false,
                        DisplayOrder = 6,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.SchemaFields.AddRange(schemaFields);
                await context.SaveChangesAsync();

                logger.LogInformation("✅ Default schema seeded successfully!");
            }
        }

        logger.LogInformation("✅ Database seeding completed!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding database");
        throw;
    }
}
