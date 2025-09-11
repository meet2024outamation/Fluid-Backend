using SharedKernel.Services;

namespace Xtract.API.Infrastructure.Interfaces
{
    public interface IMemCacheService
    {
        Task<UserAccessDetails> GetUserById(string uniqueId);
    }
}
