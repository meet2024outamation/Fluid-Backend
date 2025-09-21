using Fluid.API.Models.Tenant;
using Fluid.Entities.IAM;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces
{
    public interface ITenantService
    {
        Task<Result<IEnumerable<Tenant>>> GetAllTenantsAsync();
        Task<Result<Tenant>> GetTenantByIdAsync(string id);
        Task<Result<Tenant>> GetTenantByIdentifierAsync(string identifier);
        Task<Result<Tenant>> CreateTenantAsync(CreateTenantRequest tenant);
        Task<Result<Tenant>> UpdateTenantAsync(Tenant tenant);
        Task<Result<bool>> DeleteTenantAsync(string id);
        Task<Result<bool>> CreateTenantDatabaseAsync(Tenant tenant);
    }
}
