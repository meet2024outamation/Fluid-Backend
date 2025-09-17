using Fluid.API.Infrastructure.Interfaces;
using Fluid.Entities.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SharedKernel.AuthorizeHandler;
using SharedKernel.Models;
using SharedKernel.Services;
using SharedKernel.Utility;
using System.Collections.Concurrent;

namespace Fluid.API.Infrastructure.Services
{
    public class MemCacheService(
    FluidIAMDbContext iamDbContext,
    //IHttpContextAccessor contextAccessor,
    IDistributedCache memoryCache,
     IOptions<AppDataOptions> options
    )
    : IMemCacheService
    {
        //private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
        private readonly AppDataOptions _options = options.Value;
        private static readonly ConcurrentBag<string> _cacheKeys = new();
        public async Task<UserAccessDetails> GetUserById(string uniqueId)
        {
            var userDetail = await memoryCache.GetAsync<UserAccessDetails>(uniqueId);
            if (userDetail != null)
            {
                return userDetail;
            }


            var user = await iamDbContext.Users
     .Where(s => s.Email.ToLower() == uniqueId.ToLower())
     .Select(s => new UserAccessDetails
     {
         Id = s.Id,
         UniqueId = s.AzureAdId,   // mapping AzureAdId → UniqueId
         Email = s.Email,
         FirstName = s.FirstName,
         LastName = s.LastName,
         MiddleName = null, // no field in entity
         UserTypeId = 1, // or map accordingly
         TeamId = null, // depends on schema
         ProjectId = null, // adjust if relation exists
         AbstractorId = null,
         ProjectName = null,
         IsActive = s.IsActive,

         // Collections
         Roles = s.UserRoleUsers
             .Select(r => new UserRoles
             {
                 RoleId = r.RoleId,
                 RoleName = r.Role.Name
             })
             .ToList(),

         // HashSets
         //Modules = s.UserRoles
         //    .SelectMany(r => r.Role.Modules.Select(m => m.ModuleName))
         //    .ToHashSet(),

         //Permissions = s.UserRoleUsers
         //    .SelectMany(r => r.Role.RolePermissions.Select(p => p.Permission.Name))
         //    .ToHashSet(),

         IsServicePrinciple = false // default since not in entity
     })
     .SingleOrDefaultAsync();


            //if (user == null) throw new UserNotFoundException();

            //var form = await _contextAccessor.HttpContext!.Request.ReadFormAsync();

            //var orderInfoJson = form["orderInfo"].ToString() ?? "{}";


            //var orderInfo = JObject.Parse(orderInfoJson);

            //user.ProjectName = orderInfo["project"]?.ToString();


            if (user == null) throw new UserNotFoundException();

            await memoryCache.SetAsync(uniqueId, user, new DistributedCacheEntryOptions());
            _cacheKeys.Add(uniqueId);
            return await GetUserById(uniqueId);
        }
    }
}
