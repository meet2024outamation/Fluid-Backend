using SharedKernel.Result;
using Fluid.API.Models.Project;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IProjectService
{
    Task<Result<ProjectResponse>> CreateAsync(CreateProjectRequest request, int currentUserId);
    Task<Result<List<ProjectListResponse>>> GetAllAsync();
    Task<Result<ProjectResponse>> GetByIdAsync(int id);
    Task<Result<ProjectResponse>> UpdateAsync(int id, UpdateProjectRequest request);
    Task<Result<ProjectResponse>> UpdateStatusAsync(int id, UpdateProjectStatusRequest request);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<ProjectSchemaAssignmentResponse>> AssignSchemasAsync(AssignSchemasRequest request, int currentUserId);
}