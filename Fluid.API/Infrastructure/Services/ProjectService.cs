using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;
using Fluid.Entities.Context;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly FluidDbContext _context;
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(FluidDbContext context, FluidIAMDbContext iamContext, ILogger<ProjectService> logger)
    {
        _context = context;
        _iamContext = iamContext;
        _logger = logger;
    }

    public async Task<Result<ProjectResponse>> CreateAsync(CreateProjectRequest request, int currentUserId)
    {
        try
        {
            // Check if project code already exists
            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(c => c.Code == request.Code);

            if (existingProject != null)
            {
                _logger.LogWarning("Attempted to create project with existing code: {Code}", request.Code);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Code),
                    ErrorMessage = $"Project with code '{request.Code}' already exists."
                };

                return Result<ProjectResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var project = new Fluid.Entities.Entities.Project
            {
                Name = request.Name,
                Code = request.Code,
                IsActive = request.IsActive,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Load the created project with user information
            var createdProject = await _context.Projects
                //.Include(c => c.CreatedByUser)
                .FirstAsync(c => c.Id == project.Id);

            var response = new ProjectResponse
            {
                Id = createdProject.Id,
                Name = createdProject.Name,
                Code = createdProject.Code,
                IsActive = createdProject.IsActive,
                CreatedAt = createdProject.CreatedAt,
                UpdatedAt = createdProject.UpdatedAt,
                CreatedBy = createdProject.CreatedBy,
                //CreatedByName = createdProject.CreatedByUser.Name
            };

            _logger.LogInformation("Project created successfully with ID: {ProjectId}", createdProject.Id);
            return Result<ProjectResponse>.Created(response, "Project created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project with code: {Code}", request.Code);
            return Result<ProjectResponse>.Error("An error occurred while creating the project.");
        }
    }

    public async Task<Result<List<ProjectListResponse>>> GetAllAsync()
    {
        try
        {
            var projects = await _context.Projects
                //.Include(c => c.CreatedByUser)
                .OrderBy(c => c.Name)
                .Select(c => new ProjectListResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} projects", projects.Count);
            return Result<List<ProjectListResponse>>.Success(projects, $"Retrieved {projects.Count} projects successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects");
            return Result<List<ProjectListResponse>>.Error("An error occurred while retrieving projects.");
        }
    }

    public async Task<Result<TenantProjectsResponse>> GetAllByTenantsAsync()
    {
        try
        {
            _logger.LogInformation("?? Starting tenant-based project retrieval...");

            // Get all active tenants from IAM database
            var tenants = await _iamContext.Tenants
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            _logger.LogInformation("?? Found {TenantCount} active tenants", tenants.Count);

            var tenantsWithProjects = new List<TenantWithProjects>();
            int totalProjects = 0;

            foreach (var tenant in tenants)
            {
                try
                {
                    _logger.LogDebug("?? Processing tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Identifier);

                    if (string.IsNullOrEmpty(tenant.ConnectionString))
                    {
                        _logger.LogWarning("?? Tenant {TenantName} has no connection string, skipping...", tenant.Name);

                        // Add tenant with empty projects list
                        tenantsWithProjects.Add(new TenantWithProjects
                        {
                            TenantId = tenant.Id,
                            TenantName = tenant.Name,
                            TenantIdentifier = tenant.Identifier,
                            Description = tenant.Description,
                            IsActive = tenant.IsActive,
                            Projects = new List<ProjectInTenant>()
                        });
                        continue;
                    }

                    // Validate connection string format
                    if (!tenant.ConnectionString.Contains("Database="))
                    {
                        _logger.LogWarning("?? Tenant {TenantName} has invalid connection string format, skipping...", tenant.Name);

                        tenantsWithProjects.Add(new TenantWithProjects
                        {
                            TenantId = tenant.Id,
                            TenantName = tenant.Name,
                            TenantIdentifier = tenant.Identifier,
                            Description = tenant.Description,
                            IsActive = tenant.IsActive,
                            Projects = new List<ProjectInTenant>()
                        });
                        continue;
                    }

                    // Create tenant-specific database context
                    var tenantDbOptions = new DbContextOptionsBuilder<FluidDbContext>()
                        .UseNpgsql(tenant.ConnectionString)
                        .UseSnakeCaseNamingConvention()
                        .Options;

                    using var tenantContext = new FluidDbContext(tenantDbOptions, tenant);

                    // Test connection before querying
                    var canConnect = await tenantContext.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        _logger.LogWarning("?? Cannot connect to database for tenant: {TenantName}, skipping...", tenant.Name);

                        tenantsWithProjects.Add(new TenantWithProjects
                        {
                            TenantId = tenant.Id,
                            TenantName = tenant.Name,
                            TenantIdentifier = tenant.Identifier,
                            Description = tenant.Description,
                            IsActive = tenant.IsActive,
                            Projects = new List<ProjectInTenant>()
                        });
                        continue;
                    }

                    // Get projects for this tenant
                    var projects = await tenantContext.Projects
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.Name)
                        .Select(p => new ProjectInTenant
                        {
                            ProjectId = p.Id,
                            ProjectName = p.Name,
                            ProjectCode = p.Code,
                            IsActive = p.IsActive,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt,
                            CreatedBy = p.CreatedBy
                        })
                        .ToListAsync();

                    var tenantWithProjects = new TenantWithProjects
                    {
                        TenantId = tenant.Id,
                        TenantName = tenant.Name,
                        TenantIdentifier = tenant.Identifier,
                        Description = tenant.Description,
                        IsActive = tenant.IsActive,
                        Projects = projects
                    };

                    tenantsWithProjects.Add(tenantWithProjects);
                    totalProjects += projects.Count;

                    _logger.LogDebug("? Retrieved {ProjectCount} projects for tenant: {TenantName}", projects.Count, tenant.Name);
                }
                catch (Exception tenantEx)
                {
                    _logger.LogWarning(tenantEx, "? Failed to retrieve projects for tenant: {TenantName} - {ErrorMessage}",
                        tenant.Name, tenantEx.Message);

                    // Add tenant with empty projects list on error
                    tenantsWithProjects.Add(new TenantWithProjects
                    {
                        TenantId = tenant.Id,
                        TenantName = tenant.Name,
                        TenantIdentifier = tenant.Identifier,
                        Description = tenant.Description,
                        IsActive = tenant.IsActive,
                        Projects = new List<ProjectInTenant>()
                    });
                }
            }

            var response = new TenantProjectsResponse
            {
                Tenants = tenantsWithProjects,
                TotalTenants = tenantsWithProjects.Count,
                TotalProjects = totalProjects
            };

            _logger.LogInformation("?? Project retrieval completed: {TenantCount} tenants, {ProjectCount} total projects",
                response.TotalTenants, response.TotalProjects);

            return Result<TenantProjectsResponse>.Success(response,
                $"Retrieved {response.TotalTenants} tenants with {response.TotalProjects} total projects");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "?? Error retrieving tenant-based projects: {ErrorMessage}", ex.Message);
            return Result<TenantProjectsResponse>.Error("An error occurred while retrieving projects by tenants.");
        }
    }

    public async Task<Result<ProjectResponse>> GetByIdAsync(int id)
    {
        try
        {
            var project = await _context.Projects
                //.Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found", id);
                return Result<ProjectResponse>.NotFound();
            }

            var response = new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Code = project.Code,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                CreatedBy = project.CreatedBy,
                //CreatedByName = project.CreatedByUser.Name
            };

            _logger.LogInformation("Retrieved project with ID: {ProjectId}", id);
            return Result<ProjectResponse>.Success(response, "Project retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project with ID: {ProjectId}", id);
            return Result<ProjectResponse>.Error("An error occurred while retrieving the project.");
        }
    }

    public async Task<Result<ProjectResponse>> UpdateAsync(int id, UpdateProjectRequest request)
    {
        try
        {
            var project = await _context.Projects
                //.Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for update", id);
                return Result<ProjectResponse>.NotFound();
            }

            // Check if the new code conflicts with another project
            if (request.Code != project.Code)
            {
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(c => c.Code == request.Code && c.Id != id);

                if (existingProject != null)
                {
                    _logger.LogWarning("Attempted to update project {ProjectId} with existing code: {Code}", id, request.Code);

                    var validationError = new ValidationError
                    {
                        Key = nameof(request.Code),
                        ErrorMessage = $"Project with code '{request.Code}' already exists."
                    };

                    return Result<ProjectResponse>.Invalid(new List<ValidationError> { validationError });
                }
            }

            // Update the project properties
            project.Name = request.Name;
            project.Code = request.Code;
            project.IsActive = request.IsActive;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Code = project.Code,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                CreatedBy = project.CreatedBy,
                //CreatedByName = project.CreatedByUser.Name
            };

            _logger.LogInformation("Project updated successfully with ID: {ProjectId}", id);
            return Result<ProjectResponse>.Success(response, "Project updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project with ID: {ProjectId}", id);
            return Result<ProjectResponse>.Error("An error occurred while updating the project.");
        }
    }

    public async Task<Result<ProjectResponse>> UpdateStatusAsync(int id, UpdateProjectStatusRequest request)
    {
        try
        {
            var project = await _context.Projects
                //.Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for status update", id);
                return Result<ProjectResponse>.NotFound();
            }

            // Update only the status
            project.IsActive = request.IsActive;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Code = project.Code,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                CreatedBy = project.CreatedBy,
                //CreatedByName = project.CreatedByUser.Name
            };

            _logger.LogInformation("Project status updated successfully for ID: {ProjectId}, IsActive: {IsActive}", id, request.IsActive);
            return Result<ProjectResponse>.Success(response, "Project status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project status for ID: {ProjectId}", id);
            return Result<ProjectResponse>.Error("An error occurred while updating the project status.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var project = await _context.Projects
                .Include(c => c.Batches)
                .Include(c => c.FieldMappings)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Check if project has associated batches
            if (project.Batches.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Project",
                    ErrorMessage = "Cannot delete project as it has associated batches. Please remove all batches first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if project has associated field mappings
            if (project.FieldMappings.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "Project",
                    ErrorMessage = "Cannot delete project as it has associated field mappings. Please remove all field mappings first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Remove the project
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project deleted successfully with ID: {ProjectId}", id);
            return Result<bool>.Success(true, "Project deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project with ID: {ProjectId}", id);
            return Result<bool>.Error("An error occurred while deleting the project.");
        }
    }

    public async Task<Result<ProjectSchemaAssignmentResponse>> AssignSchemasAsync(AssignSchemasRequest request, int currentUserId)
    {
        try
        {
            // Validate project exists
            var project = await _context.Projects
                .FirstOrDefaultAsync(c => c.Id == request.ProjectId);

            if (project == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.ProjectId),
                    ErrorMessage = "Project not found."
                };
                return Result<ProjectSchemaAssignmentResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate all schemas exist and are active
            var schemas = await _context.Schemas
                .Where(s => request.SchemaIds.Contains(s.Id))
                .ToListAsync();

            var foundSchemaIds = schemas.Select(s => s.Id).ToList();
            var missingSchemaIds = request.SchemaIds.Except(foundSchemaIds).ToList();

            if (missingSchemaIds.Any())
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.SchemaIds),
                    ErrorMessage = $"Schemas with IDs [{string.Join(", ", missingSchemaIds)}] not found."
                };
                return Result<ProjectSchemaAssignmentResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var errors = new List<string>();
            var assignedSchemas = new List<AssignedSchemaInfo>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Remove existing schema assignments for this project
                var existingAssignments = await _context.ProjectSchemas
                    .Where(cs => cs.ProjectId == request.ProjectId)
                    .ToListAsync();

                if (existingAssignments.Any())
                {
                    _context.ProjectSchemas.RemoveRange(existingAssignments);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} existing schema assignments for project {ProjectId}",
                        existingAssignments.Count, request.ProjectId);
                }

                // Create new schema assignments
                foreach (var schema in schemas)
                {
                    try
                    {
                        var projectSchema = new Fluid.Entities.Entities.ProjectSchema
                        {
                            ProjectId = request.ProjectId,
                            SchemaId = schema.Id,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ProjectSchemas.Add(projectSchema);

                        assignedSchemas.Add(new AssignedSchemaInfo
                        {
                            SchemaId = schema.Id,
                            SchemaName = schema.Name,
                            Description = schema.Description,
                            IsActive = schema.IsActive,
                            AssignedAt = projectSchema.CreatedAt
                        });

                        _logger.LogDebug("KeyingInProgress schema {SchemaId} to project {ProjectId}", schema.Id, request.ProjectId);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to assign schema '{schema.Name}' (ID: {schema.Id}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to assign schema {SchemaId} to project {ProjectId}", schema.Id, request.ProjectId);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new ProjectSchemaAssignmentResponse
                {
                    ProjectId = request.ProjectId,
                    ProjectName = project.Name,
                    TotalAssignedSchemas = assignedSchemas.Count,
                    AssignedSchemas = assignedSchemas.OrderBy(a => a.SchemaName).ToList(),
                    Errors = errors
                };

                _logger.LogInformation("Schema assignment completed for project {ProjectId}. KeyingInProgress: {AssignedCount}, Errors: {ErrorCount}",
                    request.ProjectId, assignedSchemas.Count, errors.Count);

                return Result<ProjectSchemaAssignmentResponse>.Success(response,
                    $"Successfully assigned {assignedSchemas.Count} schemas to project" +
                    (errors.Any() ? $" with {errors.Count} errors" : ""));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning schemas to project {ProjectId}", request.ProjectId);
            return Result<ProjectSchemaAssignmentResponse>.Error("An error occurred while assigning schemas to the project.");
        }
    }
}