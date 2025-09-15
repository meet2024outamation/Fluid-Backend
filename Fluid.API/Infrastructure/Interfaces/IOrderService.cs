using SharedKernel.Result;
using Fluid.API.Models.Order;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IOrderService
{
    Task<Result<AssignOrderResponse>> AssignOrderAsync(AssignOrderRequest request, int currentUserId);
    Task<Result<UpdateSchemaFieldValueResponse>> UpdateSchemaFieldValueAsync(UpdateSchemaFieldValueRequest request, int currentUserId, string? ipAddress = null, string? userAgent = null);
    Task<Result<OrderListPagedResponse>> GetOrdersAsync(OrderListRequest request, int currentUserId);
}