using Fluid.API.Models.OrderStatus;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IOrderStatusService
{
    Task<Result<OrderStatusResponse>> CreateAsync(CreateOrderStatusRequest request, int currentUserId);
    Task<Result<OrderStatusResponse>> UpdateAsync(int id, UpdateOrderStatusRequest request, int currentUserId);
    Task<Result<OrderStatusResponse>> GetByIdAsync(int id);
    Task<Result<List<OrderStatusResponse>>> GetAllAsync();
    Task<Result<bool>> DeleteAsync(int id);
}