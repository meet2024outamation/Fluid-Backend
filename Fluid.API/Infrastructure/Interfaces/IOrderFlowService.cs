using Fluid.API.Models.OrderFlow;
using SharedKernel.Result;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fluid.API.Infrastructure.Interfaces;

public interface IOrderFlowService
{
    Task<Result<OrderFlowResponse>> CreateAsync(CreateOrderFlowRequest request, int currentUserId);
    Task<Result<List<OrderFlowResponse>>> CreateFlowAsync(List<CreateOrderFlowStepRequest> steps, int createdBy);
    Task<Result<OrderFlowResponse>> UpdateAsync(int id, UpdateOrderFlowRequest request, int currentUserId);
    Task<Result<OrderFlowResponse>> GetByIdAsync(int id);
    Task<Result<List<OrderFlowResponse>>> GetAllAsync();
}