using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using Fluid.Entities.Entities;
using Fluid.Entities.IAM;
using Microsoft.EntityFrameworkCore;

namespace Fluid.Entities.Context;

public class FluidDbContext : DbContext
{
    private readonly ITenantInfo? _tenantInfo;

    public FluidDbContext(DbContextOptions<FluidDbContext> options, ITenantInfo? tenantInfo = null)
        : base(options)
    {
        _tenantInfo = tenantInfo;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Apply tenant-specific connection string if available
        if (_tenantInfo is Tenant tenant && !string.IsNullOrEmpty(tenant.ConnectionString))
        {
            optionsBuilder.UseNpgsql(tenant.ConnectionString);
        }

        // Common settings
        optionsBuilder.UseSnakeCaseNamingConvention();

        base.OnConfiguring(optionsBuilder);
    }

    // DbSets for tenant-specific entities only
    public DbSet<Project> Projects { get; set; }
    public DbSet<Entities.Schema> Schemas { get; set; }
    public DbSet<Entities.SchemaField> SchemaFields { get; set; }
    public DbSet<ProjectSchema> ProjectSchemas { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderData> OrderData { get; set; }
    public DbSet<OrderFlow> OrderFlows { get; set; }
    //public DbSet<Entities.OrderStatus> OrderStatuses { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<FieldMapping> FieldMappings { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Project entity
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.IsActive)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            //entity.HasOne(e => e.CreatedByUser)
            //      .WithMany(u => u.CreatedProjects)
            //      .HasForeignKey(e => e.CreatedBy)
            //      .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Schema entity
        modelBuilder.Entity<Entities.Schema>(entity =>
        {
            //entity.Property(e => e.Id).ValueGeneratedOnAdd();
            //entity.HasOne(e => e.CreatedByUser)
            //      .WithMany(u => u.CreatedSchemas)
            //      .HasForeignKey(e => e.CreatedBy)
            //      .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SchemaField entity
        modelBuilder.Entity<Entities.SchemaField>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.SchemaId, e.FieldName }).IsUnique();
            entity.HasIndex(e => new { e.SchemaId, e.DisplayOrder }).IsUnique();

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.SchemaFields)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ProjectSchema entity
        modelBuilder.Entity<ProjectSchema>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.ProjectId, e.SchemaId }).IsUnique();

            entity.HasOne(e => e.Project)
                  .WithMany(c => c.ProjectSchemas)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.ProjectSchemas)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Batch entity
        modelBuilder.Entity<Batch>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(e => e.Project)
                  .WithMany(c => c.Batches)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Note: CreatedByUser references will be handled by UserId only
        });

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            // Configure OrderIdentifier as a required string
            entity.Property(e => e.OrderIdentifier)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.HasOne(e => e.Batch)
                  .WithMany(b => b.Orders)
                  .HasForeignKey(e => e.BatchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Project)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Removed OrderStatus navigation property configuration
            // Note: AssignedUser references will be handled by UserId only
        });

        // Configure OrderData entity
        modelBuilder.Entity<OrderData>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.OrderId, e.SchemaFieldId }).IsUnique();

            entity.HasOne(e => e.Order)
                  .WithMany(w => w.OrderData)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SchemaField)
                  .WithMany(s => s.WorkItemData)
                  .HasForeignKey(e => e.SchemaFieldId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Note: VerifiedByUser references will be handled by UserId only
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Order)
                  .WithMany(w => w.Documents)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FieldMapping entity
        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Project)
                  .WithMany(c => c.FieldMappings)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.FieldMappings)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SchemaField)
                  .WithMany(sf => sf.FieldMappings)
                  .HasForeignKey(e => e.SchemaFieldId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Note: CreatedByUser references will be handled by UserId only
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Note: User references will be handled by UserId only
        });

        // Configure OrderFlow entity
        modelBuilder.Entity<OrderFlow>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

    }
}

public class FluidIAMDbContext : EFCoreStoreDbContext<Tenant>
{
    public FluidIAMDbContext(DbContextOptions<FluidIAMDbContext> options) : base(options)
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<IAM.Schema> Schemas { get; set; }
    public DbSet<IAM.SchemaField> SchemaFields { get; set; }
    public DbSet<IAM.OrderStatus> OrderStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.AzureAdId).IsUnique();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.RoleCreatedBies)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ModifiedBy)
                  .WithMany(u => u.RoleModifiedBies)
                  .HasForeignKey(e => e.ModifiedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.PermissionCreatedBies)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ModifiedBy)
                  .WithMany(u => u.PermissionModifiedBies)
                  .HasForeignKey(e => e.ModifiedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure RolePermission entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.RolePermissionCreatedBies)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ModifiedBy)
                  .WithMany(u => u.RolePermissionModifiedBies)
                  .HasForeignKey(e => e.ModifiedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<IAM.SchemaField>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.SchemaId, e.FieldName }).IsUnique();
            entity.HasIndex(e => new { e.SchemaId, e.DisplayOrder }).IsUnique();

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.SchemaFields)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<IAM.OrderStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.Name).IsUnique();
            // Removed DisplayOrder index
            // Note: CreatedBy and UpdatedBy references will be handled by UserId only
        });
    }
}