using Microsoft.Graph.Models;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IGraphService
{
    public Task<SignInCollectionResponse?> GetSignInsLogs(string signInEventType);
    public Task<Invitation?> InviteGuestUser(Entities.IAM.User user);
    public Task<AppRoleAssignment?> UserAssignment(string userId);
    public Task UpdateUser(Entities.IAM.User user);
    public Task DeleteUserByAzureAdId(string azureAdId);
    public Task DisabledUser(string azureAdId, bool accountEnabled);
    public Task<User?> GetUserByEmail(string email);
}
