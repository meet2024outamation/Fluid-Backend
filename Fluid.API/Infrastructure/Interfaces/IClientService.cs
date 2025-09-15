using SharedKernel.Result;
using Fluid.API.Models.Client;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IClientService
{
    Task<Result<ClientResponse>> CreateAsync(CreateClientRequest request, int currentUserId);
    Task<Result<List<ClientListResponse>>> GetAllAsync();
    Task<Result<ClientResponse>> GetByIdAsync(int id);
    Task<Result<ClientResponse>> UpdateAsync(int id, UpdateClientRequest request);
    Task<Result<ClientResponse>> UpdateStatusAsync(int id, UpdateClientStatusRequest request);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<ClientSchemaAssignmentResponse>> AssignSchemasAsync(AssignSchemasRequest request, int currentUserId);
}