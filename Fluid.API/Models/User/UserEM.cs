using Microsoft.EntityFrameworkCore;
using SharedKernel.Services;
using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.User
{
    public class UserParam
    {
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
        public class UserCM
        {
            public string? UniqueId { get; set; }

            [EmailAddress]
            public string Email { get; set; } = null!;
            public string FirstName { get; set; } = null!;
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = null!;
            public string? Suffix { get; set; }
            public string? PhoneNumber { get; set; }
            public bool IsActive { get; set; }
            public int UserType { get; set; }// 1 = Internal, 2 = Clients, 3 = Abstractors

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
            public string? Title { get; set; }

            [StringLength(50)]
            [Unicode(false)]
            public string? Department { get; set; }

            [StringLength(50)]
            [Unicode(false)]
            public string? SubFunction { get; set; }

            public DateOnly? StartDate { get; set; }

            [StringLength(5)]
            [Unicode(false)]
            public string? TimeZone { get; set; }

            [StringLength(200)]
            [Unicode(false)]
            public string? CompanyName { get; set; }

            public List<int> RoleIds { get; set; }
            public int? TeamId { get; set; }
            public UserCM()
            {
                RoleIds = new List<int>();
            }

        }

        public class UserVM : UserEM
        {
            public IList<UserRoleDTO> UserRoles { get; set; } = null!;
            public IList<ModuleDTO> Modules { get; set; } = null!;
            public List<string> TeamNames { get; set; } = new List<string>();
            public UserVM()
            {
                UserRoles = new List<UserRoleDTO>();
                Modules = new List<ModuleDTO>();
            }
        }


        public class UserList : UserBasicInfo
        {
            public string? PhoneNumber { get; set; }
            public string Team { get; set; }
            public int? TeamId { get; set; }
            public DateTime CreatedOn { get; set; }
            public DateTime ModifiedOn { get; set; }
        }



    }
}
