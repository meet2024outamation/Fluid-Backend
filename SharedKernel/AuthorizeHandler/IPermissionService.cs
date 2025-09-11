namespace SharedKernel.AuthorizeHandler
{
    public interface IPermissionService
    {
        Task<PermissionVM> GetPermissionsAsync();
    }
}
