namespace SharedKernel.AuthorizeHandler
{
    public class PermissionService : IPermissionService
    {
        public PermissionService()
        {
        }

        public async Task<PermissionVM> GetPermissionsAsync()
        {
            // This service is now deprecated as permission checking is handled by ICurrentUserService
            // Return empty permissions as a placeholder
            await Task.CompletedTask;
            
            return new PermissionVM
            {
                Moduels = new HashSet<string>(),
                Permissions = new HashSet<string>(),
                PlatformApps = new HashSet<PlatformAppVM>()
            };
        }
    }
}
