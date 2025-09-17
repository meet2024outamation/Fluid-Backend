using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.User;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Models;
using SharedKernel.Result;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Infrastructure.Services;

public class ManageUserService : IManageUserService
{
    private readonly FluidIAMDbContext _iamContext;
    private readonly IGraphService _graphService;
    private readonly AzureADConfig _azureADConfig;
    private readonly ILogger<ManageUserService> _logger;

    public ManageUserService(
        FluidIAMDbContext iamContext,
        IGraphService graphService,
        IOptions<AzureADConfig> azureAdConfig,
        ILogger<ManageUserService> logger)
    {
        _graphService = graphService;
        _iamContext = iamContext;
        _azureADConfig = azureAdConfig.Value;
        _logger = logger;
    }

    public async Task<Result<UserEM>> CreateUser(UserCM userCM)
    {
        try
        {
            var isEmailExists = await IsEmailExists(userCM.Email, 0);
            if (!isEmailExists.IsSuccess)
            {
                return Result<UserEM>.Invalid(isEmailExists.ValidationErrors);
            }

            using var transaction = await _iamContext.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    FirstName = userCM.FirstName,
                    LastName = userCM.LastName,
                    Email = userCM.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var azureAdUser = await _graphService.GetUserByEmail(user.Email);

                if (azureAdUser == null)
                {
                    var invitation = await _graphService.InviteGuestUser(user);

                    if (invitation == null || invitation.InvitedUser == null || invitation.InvitedUser.Id == null)
                        return Result<UserEM>.Invalid(new List<ValidationError>
                        {
                            new ValidationError { Key = "User", ErrorMessage = $"{userCM.Email} is not added in Azure AD." }
                        });

                    user.AzureAdId = invitation.InvitedUser.Id;
                }
                else
                {
                    user.AzureAdId = azureAdUser.Id;
                }

                if (!string.IsNullOrWhiteSpace(user.AzureAdId))
                    await _graphService.UserAssignment(user.AzureAdId);

                await _iamContext.Users.AddAsync(user);
                await _iamContext.SaveChangesAsync();

                // Assign roles if provided
                if (userCM.RoleIds?.Count > 0)
                {
                    var userRoles = userCM.RoleIds.Select(roleId => new UserRole
                    {
                        RoleId = roleId,
                        UserId = user.Id,
                        CreatedDateTime = DateTimeOffset.UtcNow
                    }).ToList();

                    await _iamContext.UserRoles.AddRangeAsync(userRoles);
                    await _iamContext.SaveChangesAsync();
                }

                // Send user invitation email (simplified - using minimal template)
                var userObj = new
                {
                    DisplayName = $"{userCM.FirstName} {userCM.LastName}",
                    InviteLink = _azureADConfig.InviteRedirectUrl,
                    AppName = _azureADConfig.ClientAppName
                };

                // Note: EmailTemplate and email sending would need proper configuration
                // For now, just log the invitation
                //_logger.LogInformation("User invitation would be sent to {Email}", userCM.Email);

                await transaction.CommitAsync();

                var userDetailDto = new UserEM
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                };

                return Result<UserEM>.Created(userDetailDto, "User created successfully");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Result<UserEM>.Error("User creation failed.");
        }
    }

    public async Task<Result<UserEM>> GetUserById(int id)
    {
        try
        {
            var existingUser = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                return Result<UserEM>.NotFound();

            var userDetailDto = new UserEM
            {
                Id = existingUser.Id,
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                Email = existingUser.Email
            };

            return Result<UserEM>.Success(userDetailDto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user");
            return Result<UserEM>.Error("An error occurred while retrieving the user.");
        }
    }

    public async Task<Result<bool>> IsEmailExists(string email, int userId)
    {
        try
        {
            var existingUser = await _iamContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);

            if (existingUser != null)
            {
                return Result<bool>.Invalid(new List<ValidationError>
                {
                    new ValidationError { Key = "User", ErrorMessage = $"{email} already has an account." }
                });
            }

            return Result<bool>.Success(false, "Email is available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence");
            return Result<bool>.Error("An error occurred while checking email availability.");
        }
    }

    public async Task<IEnumerable<UserParam.UserList>> ListUsersAsync()
    {
        try
        {
            var users = await _iamContext.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive)
                .ToListAsync();

            return users.Select(u => new UserParam.UserList
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedOn = u.CreatedAt,
                ModifiedOn = u.UpdatedAt ?? u.CreatedAt,
                Team = string.Empty, // Can be populated if team functionality is added
                TeamId = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users");
            return new List<UserParam.UserList>();
        }
    }

    public async Task<Result<UserCM>> UpdateUser(UserEM userEM)
    {
        try
        {
            var existingUser = await _iamContext.Users
                .Include(s => s.UserRoleUsers)
                .FirstOrDefaultAsync(u => u.Id == userEM.Id);

            if (existingUser == null)
                return Result<UserCM>.NotFound();

            if (!existingUser.IsActive)
                return Result<UserCM>.Invalid(new List<ValidationError>
                {
                    new ValidationError { Key = "User", ErrorMessage = "You cannot change inactive user data." }
                });

            existingUser.FirstName = userEM.FirstName;
            existingUser.LastName = userEM.LastName;
            existingUser.Email = userEM.Email;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();

            var userDetailDto = new UserCM
            {
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                Email = existingUser.Email
            };

            return Result<UserCM>.Success(userDetailDto, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return Result<UserCM>.Error("An error occurred while updating the user.");
        }
    }

    public async Task<Result<int>> UpdateUserStatusAsync(int userId, bool isActive)
    {
        try
        {
            var existingUser = await _iamContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null || string.IsNullOrWhiteSpace(existingUser.AzureAdId))
                return Result<int>.NotFound();

            await _graphService.DisabledUser(existingUser.AzureAdId, isActive);
            existingUser.IsActive = isActive;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _iamContext.SaveChangesAsync();
            return Result<int>.Success(existingUser.Id, "User status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status");
            return Result<int>.Error("An error occurred while updating user status.");
        }
    }
}
