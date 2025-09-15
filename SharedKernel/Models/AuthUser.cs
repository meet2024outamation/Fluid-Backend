using Fluid.Entities.Context;
using Microsoft.AspNetCore.Http;
using SharedKernel.AuthorizeHandler;
using SharedKernel.Services;
using System.Security.Claims;


namespace SharedKernel.Models
{
    public class AuthUser : IUser
    {
        private readonly FluidDbContext _appDbConext;

        //private readonly IMemCacheService _cacheService;

        public int Id { get; private set; }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public string? Email { get; private set; }

        //public string? CurrentTenantId { get; private set; }

        //public string? CurrentTenantCode { get; private set; }

        public bool IsActive { get; private set; }
        public string? ConnectionString { get; private set; }

        public HashSet<string> Modules { get; private set; }

        public HashSet<string> Permissions { get; private set; }
        public bool IsServicePrinciple { get; set; } = false;
        public string? ClientName { get; set; }
        public int? ClientId { get; set; }
        public int? AbstractorId { get; set; }

        public string? IpAddress { get; set; }
        public int? UserTypeId { get; set; }
        public int? TeamId { get; set; }
        public ICollection<UserRoles> Roles { get; set; }

        public AuthUser(FluidDbContext appDbConext, IHttpContextAccessor httpContextAccessor)
        {
            //return;
            _appDbConext = appDbConext;
            Permissions = [];
            Modules = [];
            UserAccessDetails? userDetails = new();

            if (httpContextAccessor.HttpContext == null || httpContextAccessor.HttpContext.User.Identity == null || !httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                throw new UserNotFoundException();

            var user = httpContextAccessor.HttpContext.User;
            var claimValue = GetClaimValueFromToken(user.Claims);
            var isServicePrinciple = claimValue == 1;

            //_cacheService = cacheService;

            //if (claimValue == 1)
            //{

            //    userDetails = _cacheService.GetClientById(user.FindFirst("azp")?.Value!).Result;
            //}
            //else
            //{

            //    userDetails = _cacheService.GetUserById(user.FindFirst("preferred_username")?.Value!).Result;
            //}
            if (userDetails is null) throw new UserNotFoundException();

            if (userDetails != null)
            {
                Id = userDetails.Id;
                FirstName = userDetails.FirstName;
                LastName = userDetails.LastName;
                IsActive = userDetails.IsActive;
                Email = userDetails.Email;
                //Modules = userDetails.Modules;
                Permissions = userDetails.Permissions;
                IsServicePrinciple = isServicePrinciple;
                ClientName = userDetails?.ClientName;
                ClientId = userDetails?.ClientId;
                AbstractorId = userDetails?.AbstractorId;
                IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                UserTypeId = userDetails.UserTypeId;
                Roles = userDetails.Roles;
                TeamId = userDetails.TeamId;
            }
        }

        private int GetClaimValueFromToken(IEnumerable<Claim> claims)
        {
            if (claims is null)
                throw new UserNotFoundException();

            var claim = claims.FirstOrDefault(c => c.Type.Equals("appidacr", StringComparison.OrdinalIgnoreCase)
                                                   || c.Type.Equals("azpacr", StringComparison.OrdinalIgnoreCase));

            if (claim != null && int.TryParse(claim.Value, out var value))
            {
                return value;
            }
            throw new UserNotFoundException();
        }
    }
}