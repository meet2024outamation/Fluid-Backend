using SharedKernel.Services;
using System.Net.Http.Json;

namespace SharedKernel.AuthorizeHandler
{
    public class PermissionService : AuthService, IPermissionService
    {

        public PermissionService(IUser user) : base(user)
        {
        }

        public async Task<PermissionVM> GetPermissionsAsync()
        {
            return new PermissionVM
            {
                Moduels = _user.Modules,
                Permissions = _user.Permissions,
            };
        }
    }
}
