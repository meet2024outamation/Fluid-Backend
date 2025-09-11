using SharedKernel.Result;
using Xtract.API.Models.Order;

namespace Xtract.API.Infrastructure.Interfaces;

public interface IOrderService
{
    Task<Result<AssignOrderResponse>> AssignOrderAsync(AssignOrderRequest request, int currentUserId);
}