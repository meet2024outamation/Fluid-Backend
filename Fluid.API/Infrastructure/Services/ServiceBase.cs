using Fluid.Entities.Context;
using Fluid.API.Infrastructure.Interfaces;

namespace Fluid.API.Infrastructure.Services
{
    public class ServiceBase
    {
        protected readonly FluidDbContext _context;
        protected readonly ICurrentUserService _currentUserService;

        public ServiceBase(FluidDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        
        /// <summary>
        /// Gets the current user's ID
        /// </summary>
        protected int GetCurrentUserId() => _currentUserService.GetCurrentUserId();
        
        /// <summary>
        /// Gets the current user's name
        /// </summary>
        protected string GetCurrentUserName() => _currentUserService.GetCurrentUserName();
        
        /// <summary>
        /// Gets the current user's email
        /// </summary>
        protected string GetCurrentUserEmail() => _currentUserService.GetCurrentUserEmail();
    }
}
