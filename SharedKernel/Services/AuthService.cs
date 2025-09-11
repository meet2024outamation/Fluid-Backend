using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Services
{
    public interface IUser
    {
        public int Id { get; }//bind UserId or ServicePrincipalId
        public string? FirstName { get; }
        public string? LastName { get; }
        public string Name => $"{FirstName} {LastName}";

        public string? Email { get; }

        public bool IsActive { get; }
        public string? ConnectionString { get; }
        public HashSet<string> Modules { get; }
        public HashSet<string> Permissions { get; }
        public bool IsServicePrinciple { get; set; }
        public string? ClientName { get; set; }
        public int? ClientId { get; set; }
        public int? AbstractorId { get; set; }

        public string? IpAddress { get; set; }
        public int? UserTypeId { get; set; }
        public int? TeamId { get; set; }
        public ICollection<UserRoles> Roles { get; set; }
    }



    public class TokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<TokenHandler> logger)
        : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var token = ExtractToken();
            logger.LogInformation(new { call = "Share AuthService:", Data = token, EmailAddress = httpContextAccessor.HttpContext!.User.FindFirst("preferred_username")?.Value }.ToString());
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private string ExtractToken()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                return httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            }
            return string.Empty;
        }
    }

    public class AuthService(IUser user)
    {
        protected readonly IUser _user = user;
    }
    public class UserRoles
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class UserBasicInfo
    {
        public int Id { get; set; }
        public string? UniqueId { get; set; }
        public string Name => $"{FirstName} {LastName}";
        public ICollection<UserRoles> Roles { get; set; } = new List<UserRoles>();
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        //public int UserType { get; set; }// 1 = Internal, 2 = Clients, 3 = Abstractors
        public int UserTypeId { get; set; }

        public string UserType =>
            UserTypeId switch
            {
                1 => "internal",
                2 => "customer",
                3 => "abstractor",
                _ => "unknown"
            };
        public int? TeamId { get; set; }
        public int? ClientId { get; set; }
        public int? AbstractorId { get; set; }

        public string? ClientName { get; set; }

        public bool IsActive { get; set; }
    }
    public class UserAccessDetails : UserBasicInfo
    {
        //public string ConnectionString { get; set; } = null!;
        //public string? ServicePrincipalName { get; set; }
        //public HashSet<string> Modules { get; set; } = null!;
        public HashSet<string> Permissions { get; set; } = null!;
        //public string CurrentTenantCode { get; set; } = null!;
        public bool IsServicePrinciple { get; set; } = false;
        public string? ClientName { get; set; }
        public int? ClientId { get; set; }

    }



    public class UrlsConfig
    {
        public string IAMUrl { get; set; } = string.Empty;

    }


    public class TokenOptions
    {
        public string Name { get; set; } = string.Empty;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string Scope { get; set; } = null!;
        public string GrantType { get; set; } = null!;
        public string TokenUrl { get; set; } = null!;
    }
}
