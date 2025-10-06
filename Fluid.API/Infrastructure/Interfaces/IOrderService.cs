using Fluid.API.Models.Order;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IOrderService
{
    Task<Result<OrderListPagedResponse>> GetOrdersAsync(OrderListRequest request);
    Task<Result<OrderDto>> GetOrderByIdAsync(int orderId);
    Task<Result<AssignOrderResponse>> AssignOrderAsync(AssignOrderRequest request, int currentUserId);
    Task<Result<UpdateSchemaFieldValueResponse>> UpdateSchemaFieldValueAsync(UpdateSchemaFieldValueRequest request, int currentUserId, string? ipAddress = null, string? userAgent = null);
}