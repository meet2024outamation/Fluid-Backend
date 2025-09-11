using Microsoft.AspNetCore.Http;
using SharedKernel.AuthorizeHandler;
using SharedKernel.Services;
using Microsoft.Extensions.Caching.Distributed;


namespace SharedKernel.Models
{
    public class AuthUser : IUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;

        public AuthUser(IHttpContextAccessor httpContextAccessor, IDistributedCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        private UserDetail UserDetails =>
            _httpContextAccessor.HttpContext?.Items["UserDetails"] as UserDetail
            ?? throw new UserNotFoundException();

        public int Id => UserDetails.Id;

        public string? FirstName => UserDetails.FirstName;
        public string? LastName => UserDetails.LastName;
        public string? Email => UserDetails.Email;
        public HashSet<string> Modules => UserDetails.Modules;
        public HashSet<string> Permissions => UserDetails.Permissions;
        public string? UserStatus => UserDetails.UserStatus;
        public bool IsServicePrincipal => UserDetails.IsServicePrincipal;
        }
}
