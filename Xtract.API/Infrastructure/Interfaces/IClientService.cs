using SharedKernel.Result;
using Xtract.API.Models.Client;

namespace Xtract.API.Infrastructure.Interfaces;

public interface IClientService
{
    Task<Result<ClientResponse>> CreateAsync(CreateClientRequest request, int currentUserId);
    Task<Result<List<ClientListResponse>>> GetAllAsync();
    Task<Result<ClientResponse>> GetByIdAsync(int id);
    Task<Result<ClientResponse>> UpdateAsync(int id, UpdateClientRequest request);
}