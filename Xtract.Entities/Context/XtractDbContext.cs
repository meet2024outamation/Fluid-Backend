using Microsoft.EntityFrameworkCore;
using Xtract.Entities.Entities;

namespace Xtract.Entities.Context;

public class XtractDbContext : DbContext
{
    public XtractDbContext(DbContextOptions<XtractDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Schema> Schemas { get; set; }
    public DbSet<SchemaField> SchemaFields { get; set; }
    public DbSet<ClientSchema> ClientSchemas { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Order> WorkItems { get; set; }
    public DbSet<OrderData> WorkItemData { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<FieldMapping> FieldMappings { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    // New role-related entities
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // New module-related entities

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
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure RolePermission entity (composite key)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.UserRoleCreatedBies)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ModifiedBy)
                  .WithMany(u => u.UserRoleModifiedBies)
                  .HasForeignKey(e => e.ModifiedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Client entity
        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedClients)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Schema entity
        modelBuilder.Entity<Schema>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedSchemas)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SchemaField entity
        modelBuilder.Entity<SchemaField>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DataType)
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.HasIndex(e => new { e.SchemaId, e.FieldName }).IsUnique();
            entity.HasIndex(e => new { e.SchemaId, e.DisplayOrder }).IsUnique();

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.SchemaFields)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ClientSchema entity
        modelBuilder.Entity<ClientSchema>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.ClientId, e.SchemaId }).IsUnique();

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.ClientSchemas)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.ClientSchemas)
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

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Batches)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedBatches)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure WorkItem entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasOne(e => e.Batch)
                  .WithMany(b => b.WorkItems)
                  .HasForeignKey(e => e.BatchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.WorkItems)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany(u => u.AssignedWorkItems)
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure WorkItemData entity
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

            entity.HasOne(e => e.VerifiedByUser)
                  .WithMany(u => u.VerifiedWorkItemData)
                  .HasForeignKey(e => e.VerifiedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.WorkItem)
                  .WithMany(w => w.Documents)
                  .HasForeignKey(e => e.WorkItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FieldMapping entity
        modelBuilder.Entity<FieldMapping>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Client)
                  .WithMany(c => c.FieldMappings)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Schema)
                  .WithMany(s => s.FieldMappings)
                  .HasForeignKey(e => e.SchemaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedFieldMappings)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ChangedAuditLogs)
                  .HasForeignKey(e => e.ChangedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}