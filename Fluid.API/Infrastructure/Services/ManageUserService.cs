using Microsoft.Extensions.Options;
using SharedKernel.Models;
using SharedKernel.Services;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.Entities.Context;

namespace Fluid.API.Infrastructure.Services;

public class ManageUserService(
    FluidDbContext context,
    IGraphService graphService,
    IEmailServices emailServices,
    IOptions<AzureADConfig> azureAdConfig,
    IUser _user)
    : ServiceBase(context, _user), IManageUserService
{
    //public async Task<Result<UserEM>> CreateUser(UserCM userCM)
    //{
    //    var isEmailExists = await IsEmailExists(userCM.Email, _user.Id);
    //    if (!isEmailExists.Success)
    //    {
    //        return Result<UserEM>.BadRequest(isEmailExists.ValidationErrors);
    //    }

    //    if (_user.UserTypeId == Meta.UserTypes.Internal && userCM.UserType == null)
    //    {
    //        return Result<UserEM>.BadRequest(new[]
    //        {
    //            new ValidationError()
    //            {
    //                Key = "User",
    //                ErrorMessage = "User type is required for Internal user."
    //            }
    //        });
    //    }

    //    if (_user.UserTypeId == Meta.UserTypes.Internal && (userCM.UserType == 0 || userCM.UserType > 3))
    //    {
    //        return Result<UserEM>.BadRequest(new List<ValidationError>
    //        {
    //            new ValidationError { Key = "User", ErrorMessage = "Invalid User type" }
    //        });
    //    }

    //    if (userCM.UserType == Meta.UserTypes.Client && userCM.ClientId == null)
    //    {
    //        return Result<UserEM>.BadRequest(new List<ValidationError>
    //        {
    //            new ValidationError { Key = "User", ErrorMessage = "Client Id is required for Client user." }
    //        });
    //    }

    //    if (userCM.UserType == Meta.UserTypes.Abstractor && userCM.AbstractorId == null)
    //    {
    //        return Result<UserEM>.BadRequest(new List<ValidationError>
    //        {
    //            new ValidationError { Key = "User", ErrorMessage = Meta.Abstractors.Validations.AbstractorCompany }
    //        });
    //    }

    //    using var transaction = await _context.Database.BeginTransactionAsync();

    //    var user = mapper.Map<User>(userCM);
    //    var azureAdUser = await graphService.GetUserByEmail(user.Email);
    //    if (azureAdUser == null)
    //    {
    //        var invitation = await graphService.InviteGuestUser(user);

    //        if (invitation == null || invitation.InvitedUser == null || invitation.InvitedUser.Id == null)
    //            return Result<UserEM>.BadRequest(new List<ValidationError>
    //            {
    //                new ValidationError { Key = "User", ErrorMessage = $"{userCM.Email} is not added in Azure AD." }
    //            });

    //        user.AzureADUserId = invitation.InvitedUser.Id;
    //    }
    //    else
    //    {
    //        user.AzureADUserId = azureAdUser.Id;
    //    }

    //    if (!user.AzureADUserId.IsNullOrWhiteSpace())
    //        await graphService.UserAssignment(user.AzureADUserId);

    //    if (_user.UserTypeId == Meta.UserTypes.Client)
    //    {
    //        user.UserType = Meta.UserTypes.Client;
    //        user.ClientId = _user.ClientId;
    //        var clientUserRoleId = await _context.Roles
    //            .Where(r => r.Name == Meta.Roles.CustomerUser)
    //            .Select(r => r.Id)
    //            .FirstOrDefaultAsync();
    //        userCM.RoleIds = [clientUserRoleId];
    //    }

    //    if (_user.UserTypeId == Meta.UserTypes.Abstractor)
    //    {
    //        user.UserType = Meta.UserTypes.Abstractor;
    //        user.AbstractorId = _user.AbstractorId;
    //        var clientUserRoleId = await _context.Roles
    //            .Where(r => r.Name == Meta.Roles.ABSSearcher)
    //            .Select(r => r.Id)
    //            .FirstOrDefaultAsync();
    //        userCM.RoleIds = [clientUserRoleId];
    //    }

    //    //user.UserType = 1;
    //    user.CreatedById = _user.Id;
    //    user.CreatedOn = Models.Meta.DateTimeProvider.Now;

    //    user.ModifiedById = _user.Id;
    //    user.ModifiedOn = Models.Meta.DateTimeProvider.Now;

    //    await _context.Users.AddAsync(user);
    //    await graphService.UpdateUser(user);
    //    await _context.SaveChangesAsync();

    //    if (userCM.RoleIds?.Count > 0)
    //    {
    //        var userRoles = userCM.RoleIds.Select(roleId => new UserRole
    //        { RoleId = roleId, UserId = user.Id, ModifiedById = _user.Id, ModifiedOn = DateTime.UtcNow }).ToList();
    //        await _context.UserRoles.AddRangeAsync(userRoles);

    //        var permissions = await _context.RolePermissions
    //            .Include(r => r.Permission)
    //            .ThenInclude(r => r.Module)
    //            .Where(r => r.Permission != null && userCM.RoleIds.Contains(r.RoleId)).ToListAsync();
    //        var userModules = permissions
    //            .Select(p => p.Permission!.ModuleId)
    //            .Distinct()
    //            .Select(p => new UserModule
    //            {
    //                ModuleId = p,
    //                UserId = user.Id,
    //                ModifiedById = _user.Id,
    //                ModifiedOn = DateTime.UtcNow
    //            });
    //        await _context.UserModules.AddRangeAsync(userModules);

    //        var userPermissions = permissions
    //            .Select(p => new UserPermission
    //            {
    //                PermissionId = p.PermissionId,
    //                UserId = user.Id,
    //                IsRolePermission = true,
    //                ModifiedById = _user.Id,
    //                ModifiedOn = DateTime.UtcNow
    //            });
    //        await _context.UserPermissions.AddRangeAsync(userPermissions);
    //    }

    //    //if (userCM.TeamId != null && userCM.TeamId != 0 )
    //    //{
    //    //    var teamUsers =  new TeamUser { TeamId = userCM.TeamId.Value, UserId = user.Id };
    //    //    await _context.TeamUsers.AddAsync(teamUsers);


    //    //}

    //    await _context.SaveChangesAsync();


    //    var userObj = new
    //    {
    //        DisplayName = $"{userCM.FirstName} {userCM.LastName}",
    //        InviteLink = azureAdConfig.Value.InviteRedirectUrl,
    //        AppName = azureAdConfig.Value.ClientAppName
    //    };
    //    userInvite.To.Add(userCM.Email);
    //    await emailServices.SendEmailWithTemplateAsync(userInvite, userObj);
    //    await transaction.CommitAsync();
    //    var userDetailDto = mapper.Map<UserEM>(user);
    //    return Result<UserEM>.Ok(userDetailDto);
    //}

    //public async Task<Result<int>> UpdateUserStatusAsync(int userId, bool isActive)
    //{
    //    var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);
    //    if (existingUser == null || existingUser.AzureADUserId.IsNullOrWhiteSpace())
    //        return Result<int>.BadRequest(new List<ValidationError>
    //            { new() { Key = "User", ErrorMessage = $"User is not found." } });

    //    await graphService.DisabledUser(existingUser.AzureADUserId, isActive);
    //    existingUser.IsActive = isActive;
    //    existingUser.ModifiedById = _user.Id;
    //    await _context.SaveChangesAsync();
    //    return Result<int>.Ok(existingUser.Id);
    //}

    //public Task<Result<List<UserBasicInfo>>> GetActiveUsersByRoleAsync(int roleId)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<Result<List<UserBasicInfo>>> GetActiveUsersByPermissionAsync(int permission)
    //{
    //    throw new NotImplementedException();
    //}

    //public async Task<Result<UserEM>> GetUserById(int id)
    //{
    //    var existingUser = await _context.Users /*.Include(t => t.UserTenants)*/
    //        .SingleOrDefaultAsync(u => u.Id == id);

    //    if (existingUser == null)
    //        return Result<UserEM>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"User is not found." } });

    //    return Result<UserEM>.Ok(mapper.Map<UserEM>(existingUser));
    //}

    //public async Task<Result<UserVM>> GetUserByUniqueId(string uniqueId)
    //{
    //    var existingUser = await _context.Users.ProjectTo<UserVM>(mapper.ConfigurationProvider)
    //        .SingleOrDefaultAsync(u => u.UniqueId == uniqueId);

    //    if (existingUser == null)
    //        return Result<UserVM>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"User is not found." } });

    //    return Result<UserVM>.Ok(existingUser);
    //}


    //public async Task<IEnumerable<UserList>> ListUsersAsync()
    //{
    //    var query = _context.Users
    //        .Include(u => u.Client)
    //        .Include(u => u.Abstractor)
    //        .Include(u => u.CreatedBy)
    //        .Include(u => u.ModifiedBy)
    //        .Include(u => u.Roles)
    //        .ThenInclude(ur => ur.Role)
    //        .Include(t => t.Team)
    //        .AsQueryable();

    //    var response = mapper.Map<List<Endpoints.User.UserList>>(await query.ToListAsync());
    //    return response;
    //}


    //public async Task<Result<UserCM>> UpdateUser(UserEM userEM)
    //{
    //    var existingUser = await _context.Users
    //        .Include(s => s.Roles)
    //        .Include(s => s.Team)
    //        .Include(s => s.UserPermissions)
    //        .SingleOrDefaultAsync(u => u.Id == userEM.UserId);

    //    if (existingUser == null)
    //        return Result<UserCM>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"User is not found." } });

    //    if (!existingUser.IsActive)
    //        return Result<UserCM>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"You can not change inactive user data." } });

    //    if (userEM.Body.UserType == Models.Meta.UserTypes.Internal && existingUser.Email != userEM.Body.Email)
    //        return Result<UserCM>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"You can not change email." } });

    //    existingUser = mapper.Map(userEM.Body, existingUser);
    //    await graphService.UpdateUser(existingUser);

    //    _context.UserRoles.RemoveRange(existingUser.Roles);
    //    _context.UserPermissions.RemoveRange(existingUser.UserPermissions.Where(s => s.IsRolePermission).ToList());

    //    var existingModules = await _context.UserModules
    //        .Where(um => um.UserId == existingUser.Id)
    //        .ToListAsync();
    //    _context.UserModules.RemoveRange(existingModules);

    //    if (userEM.Body.RoleIds?.Count > 0)
    //    {
    //        var userRoles = userEM.Body.RoleIds.Select(roleId => new UserRole
    //        {
    //            RoleId = roleId,
    //            UserId = existingUser.Id,
    //            ModifiedById = _user.Id,
    //            ModifiedOn = DateTime.UtcNow
    //        }).ToList();

    //        await _context.UserRoles.AddRangeAsync(userRoles);

    //        var permissions = await _context.RolePermissions
    //            .Include(r => r.Permission)
    //            .ThenInclude(r => r.Module)
    //            .Where(r => userEM.Body.RoleIds.Contains(r.RoleId)).ToListAsync();
    //        var userModules = permissions
    //            .Select(p => p.Permission.ModuleId)
    //            .Distinct()
    //            .Select(p => new UserModule
    //            {
    //                ModuleId = p,
    //                UserId = existingUser.Id,
    //                ModifiedById = _user.Id,
    //                ModifiedOn = DateTime.UtcNow
    //            });

    //        var existingModuleIds = existingModules.Select(um => um.ModuleId).ToList();
    //        var newModules = userModules
    //            .Where(um => !existingModuleIds.Contains(um.ModuleId))
    //            .ToList();

    //        await _context.UserModules.AddRangeAsync(newModules);

    //        var existingUserPermissionKeys = await _context.UserPermissions
    //            .Where(up => up.UserId == existingUser.Id && !up.IsRolePermission)
    //            .Select(up => up.PermissionId)
    //            .ToListAsync();


    //        var userPermissions = permissions
    //            .Where(p => !existingUserPermissionKeys.Contains(p.PermissionId))
    //            .Select(p => new UserPermission
    //            {
    //                PermissionId = p.PermissionId,
    //                UserId = existingUser.Id,
    //                IsRolePermission = true,
    //                ModifiedById = _user.Id,
    //                ModifiedOn = DateTime.UtcNow
    //            });
    //        await _context.UserPermissions.AddRangeAsync(userPermissions);
    //    }

    //    await _context.SaveChangesAsync();
    //    var UserDetailDto = mapper.Map<UserCM>(existingUser);
    //    return Result<UserCM>.Ok(UserDetailDto);
    //}

    //public async Task<Result<UserBasicDetailsDto>> GetUserByEmail(string email)
    //{
    //    var existingUser = await _context.Users.ProjectTo<UserBasicDetailsDto>(mapper.ConfigurationProvider)
    //        .SingleOrDefaultAsync(s => s.Email == email);

    //    if (existingUser == null)
    //        return Result<UserBasicDetailsDto>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"User is not found." } });

    //    return Result<UserBasicDetailsDto>.Ok(existingUser);
    //}

    //public async Task<Result<List<UserBasicInfo>>> GetActiveUsersByRolesAsync(string role)
    //{
    //    var result = await _context.Users.Where(u => u.IsActive && u.Roles.Any(r => r.Role.Name == role))
    //        .ProjectTo<UserBasicInfo>(mapper.ConfigurationProvider).ToListAsync();
    //    ;
    //    return Result<List<UserBasicInfo>>.Ok(result);
    //}

    //public async Task<Result<List<UserBasicInfo>>> GetActiveUsersByPermissionAsync(string permission)
    //{
    //    var result = await _context.Users
    //        .Where(u => u.IsActive
    //                    && (u.Roles.Any(r =>
    //                            r.Role != null && r.Role.RolePermissions.Any(p =>
    //                                p.Permission != null && p.Permission.Code == permission))
    //                        || u.UserPermissions.Any(p => p.Permission != null && p.Permission.Code == permission)))
    //        .ProjectTo<UserBasicInfo>(mapper.ConfigurationProvider).Distinct().ToListAsync();
    //    ;
    //    return Result<List<UserBasicInfo>>.Ok(result);
    //}

    //public async Task<Result<bool>> IsEmailExists(string email, int userId)
    //{
    //    var existingUser = await _context.Users
    //        .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    //    var currentUser = await _context.Users
    //        .SingleOrDefaultAsync(u => u.Id == userId);

    //    if (existingUser != null &&
    //        currentUser !=
    //        null
    //       )
    //    {
    //        return Result<bool>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"{email} already has an account." } });
    //    }

    //    if (existingUser != null)
    //        return Result<bool>.BadRequest(new List<ValidationError>
    //        {
    //            new ValidationError
    //                { Key = "User", ErrorMessage = $"Kindly reach out to OutamateDS Administrator to add this user." }
    //        });

    //    return Result<bool>.Ok(false);
    //}

    //public async Task<Result<UserDetail?>> GetUserDetailById(string id)
    //{
    //    var userDetail = await _context.Users
    //        .ProjectTo<UserDetail>(mapper.ConfigurationProvider)
    //        .SingleOrDefaultAsync(u => u.Email.ToLower() == id.ToLower() && u.IsActive);
    //    return userDetail;
    //}

    //public async Task<IList<string>> GetEmails()
    //{
    //    return await _context.Users.Select(s => s.Email).ToListAsync();
    //}

    //public async Task<Result<UserBasicDetailsDto>> GetUserBasicDetailsById(int id)
    //{
    //    var user = await _context.Users
    //        .ProjectTo<UserBasicDetailsDto>(mapper.ConfigurationProvider)
    //        .SingleOrDefaultAsync(u => u.Id == id);
    //    if (user == null)
    //        return Result<UserBasicDetailsDto>.BadRequest(new List<ValidationError>
    //            { new ValidationError { Key = "User", ErrorMessage = $"User is not found." } });
    //    return Result<UserBasicDetailsDto>.Ok(user);
    //}


}
