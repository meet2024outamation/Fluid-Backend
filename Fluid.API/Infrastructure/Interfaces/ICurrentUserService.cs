namespace Fluid.API.Infrastructure.Interfaces;

public interface ICurrentUserService
{
    int GetCurrentUserId();
    string GetCurrentUserName();
}