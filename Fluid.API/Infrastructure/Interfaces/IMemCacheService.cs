using SharedKernel.Services;

namespace Fluid.API.Infrastructure.Interfaces
{
    public interface IMemCacheService
    {
        Task<UserAccessDetails> GetUserById(string uniqueId);
    }
}
