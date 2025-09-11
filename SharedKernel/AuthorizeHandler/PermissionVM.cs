namespace SharedKernel.AuthorizeHandler
{
    public class PermissionVM
    {
        public HashSet<PlatformAppVM> PlatformApps { get; set; }
        public HashSet<string> Moduels { get; set; }
        public HashSet<string> Permissions { get; set; }
        public PermissionVM()
        {
            Moduels = new HashSet<string>();
            Permissions = new HashSet<string>();
            PlatformApps = new HashSet<PlatformAppVM>();
        }

    }

    public class PlatformVM
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class PlatformAppVM
    {
        public string PlatformId { get; set; } = null!;
        public string AppId { get; set; } = null!;
    }
}
