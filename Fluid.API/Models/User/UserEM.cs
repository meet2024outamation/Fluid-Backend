using Microsoft.EntityFrameworkCore;
using SharedKernel.Services;
using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.User
{
    ///// <summary>
    ///// Enumeration for user context types
    ///// </summary>
    //public enum UserContextType
    //{
    //    Global,     // ProductOwner - system-wide access
    //    Tenant,     // TenantAdmin - tenant-scoped access
    //    Project     // Keying, QC, etc. - project-scoped access
    //}

    public class UserParam
    {
        /// <summary>
        /// User response model - used for returning user data from APIs
        /// </summary>
        public class UserResponse
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string Name => $"{FirstName} {LastName}".Trim();
            public List<ProjectRoleResponse> Roles { get; set; } = new List<ProjectRoleResponse>();
        }

        /// <summary>
        /// User response model for "Me" endpoint - used for current user information
        /// </summary>
        public class UserMeResponse
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string Name => $"{FirstName} {LastName}".Trim();

            // Roles for the current context (global, tenant, or project)
            public List<UserRoleInfo> Roles { get; set; } = new List<UserRoleInfo>();

            // Permissions aggregated from roles for the current context
            public List<PermissionInfo> Permissions { get; set; } = new List<PermissionInfo>();

            // Current context information
            public string? CurrentTenantId { get; set; }
            public string? CurrentTenantName { get; set; }
            public int? CurrentProjectId { get; set; }
            public string? CurrentProjectName { get; set; }

            // Role context type
            //public UserContextType ContextType { get; set; }
        }

        /// <summary>
        /// Permission information for user response
        /// </summary>
        public class PermissionInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        /// <summary>
        /// User create/update request model - used for creating and updating users
        /// </summary>
        public class UserRequest
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(255)]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(255)]
            public string LastName { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Phone { get; set; }

            public bool IsActive { get; set; } = true;

            public List<ProjectRole> Roles { get; set; } = new List<ProjectRole>();
        }
        public class ProjectRole
        {
            public string TenantId { get; set; } = string.Empty;
            public int? ProjectId { get; set; } = null;
            public int RoleId { get; set; }
        }
        /// <summary>
        /// Project role assignment - includes tenant, project, and role information
        /// </summary>
        public class ProjectRoleResponse
        {
            public string TenantId { get; set; } = string.Empty;
            public int ProjectId { get; set; }
            public int RoleId { get; set; }
            public string? TenantName { get; set; }
            public string? ProjectName { get; set; }
            public string? RoleName { get; set; }
        }

        /// <summary>
        /// User list item - used for list/search operations
        /// </summary>
        public class UserListItem
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Name => $"{FirstName} {LastName}".Trim();
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<string> RoleNames { get; set; } = new List<string>();
        }

        /// <summary>
        /// User role information - simplified role details for "Me" endpoint
        /// </summary>
        public class UserRoleInfo
        {
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        /// <summary>
        /// User status update request
        /// </summary>
        public class UserStatusRequest
        {
            public bool IsActive { get; set; }
        }

        #region Legacy DTOs (keeping for backward compatibility)

        [Obsolete("Use UserResponse instead")]
        public class UserEM : UserCM
        {
            public int? Id { get; set; }
            public string Name
            {
                get
                {
                    return $"{LastName}, {FirstName}";
                }
            }
        }

        [Obsolete("Use UserRequest instead")]
        public class UserCM
        {
            public string? UniqueId { get; set; }

            [EmailAddress]
            public string Email { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? PhoneNumber { get; set; }
            public bool IsActive { get; set; }
            public string? LineText { get; set; }

            [StringLength(50)]
            [Unicode(false)]
            public string? AdditionalLineText { get; set; }

            [StringLength(50)]
            [Unicode(false)]
            public string? CityName { get; set; }

            [StringLength(2)]
            [Unicode(false)]
            public string? StateCode { get; set; }

            [StringLength(13)]
            [Unicode(false)]
            public string? PostalCode { get; set; }
            [StringLength(20)]
            public string? ExtPhoneNumber { get; set; }

            [StringLength(50)]
            [Unicode(false)]
            public string? SubFunction { get; set; }

            public List<int> RoleIds { get; set; }
            public int? TeamId { get; set; }
            public UserCM()
            {
                RoleIds = new List<int>();
            }
        }

        [Obsolete("Use UserResponse instead")]
        public class UserVM : UserEM
        {
            public IList<UserRoleDTOLegacy> UserRoles { get; set; } = new List<UserRoleDTOLegacy>();
            public IList<ModuleDTOLegacy> Modules { get; set; } = new List<ModuleDTOLegacy>();
            public List<string> TeamNames { get; set; } = new List<string>();
        }

        [Obsolete("Use UserListItem instead")]
        public class UserList : UserBasicInfo
        {
            public string? PhoneNumber { get; set; }
            public string Team { get; set; } = string.Empty;
            public int? TeamId { get; set; }
            public DateTime CreatedOn { get; set; }
            public DateTime ModifiedOn { get; set; }
        }

        #endregion
    }

    #region Legacy DTOs (keeping for external dependencies)

    [Obsolete("Use UserParam.UserRoleInfo instead")]
    public class UserRoleDTOLegacy
    {
        public int RoleId { get; set; }
        public int UserId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public IList<PermissionDTOLegacy> Permissions { get; set; } = new List<PermissionDTOLegacy>();
    }

    [Obsolete("Consider using a simplified module representation")]
    public class ModuleDTOLegacy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    [Obsolete("Define permissions structure as needed")]
    public class PermissionDTOLegacy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    #endregion
}
