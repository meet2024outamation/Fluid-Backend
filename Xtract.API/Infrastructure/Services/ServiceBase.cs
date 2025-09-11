using SharedKernel.Services;
using Xtract.Entities.Context;

namespace Xtract.API.Infrastructure.Services
{
    public class ServiceBase : AuthService
    {
        protected readonly XtractDbContext _context;

        public ServiceBase(XtractDbContext context, IUser user) : base(user)
        {
            _context = context;
        }
    }
}
