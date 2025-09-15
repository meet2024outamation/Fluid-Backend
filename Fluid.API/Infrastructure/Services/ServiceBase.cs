using Fluid.Entities.Context;
using SharedKernel.Services;

namespace Fluid.API.Infrastructure.Services
{
    public class ServiceBase : AuthService
    {
        protected readonly FluidDbContext _context;

        public ServiceBase(FluidDbContext context, IUser user) : base(user)
        {
            _context = context;
        }
    }
}
