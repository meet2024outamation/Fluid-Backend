using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderFlow;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class UpdatedOrderFlowService : IOrderFlowService
{
    private readonly FluidDbContext _context;
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<UpdatedOrderFlowService> _logger;

    public UpdatedOrderFlowService(FluidDbContext context, FluidIAMDbContext iamContext, ILogger<UpdatedOrderFlowService> logger)
    {
        _context = context;
        _iamContext = iamContext;
        _logger = logger;
    }

    public async Task<Result<OrderFlowResponse>> CreateAsync(CreateOrderFlowRequest request, int currentUserId)
    {
        try
        {
            // Validate that the order exists
            var orderExists = await _context.Orders
                .AnyAsync(o => o.Id == request.OrderId);

            if (!orderExists)
            {
                _logger.LogWarning("Attempted to create order flow for non-existent order: {OrderId}", request.OrderId);

                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderId),
                    ErrorMessage = $"Order with ID {request.OrderId} not found."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate that the order status exists in IAM
            var orderStatusExists = await _context.OrderStatuses
                .AnyAsync(os => os.Id == request.OrderStatusId && os.IsActive);

            if (!orderStatusExists)
            {
                _logger.LogWarning("Attempted to create order flow with non-existent or inactive order status: {OrderStatusId}", request.OrderStatusId);

                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderStatusId),
                    ErrorMessage = $"Order status with ID {request.OrderStatusId} not found or inactive."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if order flow with the same rank already exists for this order
            var existingOrderFlow = await _context.Set<OrderFlow>()
                .FirstOrDefaultAsync(of => of.OrderId == request.OrderId && of.Rank == request.Rank);

            if (existingOrderFlow != null)
            {
                _logger.LogWarning("Attempted to create order flow with existing rank {Rank} for order {OrderId}", request.Rank, request.OrderId);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Rank),
                    ErrorMessage = $"Order flow with rank {request.Rank} already exists for this order."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var orderFlow = new OrderFlow
            {
                OrderId = request.OrderId,
                OrderStatusId = request.OrderStatusId,
                Rank = request.Rank,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<OrderFlow>().Add(orderFlow);
            await _context.SaveChangesAsync();

            // Get the created order flow with related data
            var createdOrderFlow = await _context.Set<OrderFlow>()
                .Include(of => of.Order)
                    .ThenInclude(o => o.Project)
                .Include(of => of.Order)
                    .ThenInclude(o => o.Batch)
                .FirstAsync(of => of.Id == orderFlow.Id);

            // Get the order status name from IAM
            var orderStatus = await _context.OrderStatuses
                .FirstAsync(os => os.Id == orderFlow.OrderStatusId);

            var response = new OrderFlowResponse
            {
                Id = createdOrderFlow.Id,
                OrderId = createdOrderFlow.OrderId,
                OrderStatusId = createdOrderFlow.OrderStatusId,
                StatusName = orderStatus.Name,
                Rank = createdOrderFlow.Rank,
                CreatedBy = createdOrderFlow.CreatedBy,
                UpdatedBy = createdOrderFlow.UpdatedBy,
                CreatedAt = createdOrderFlow.CreatedAt,
                UpdatedAt = createdOrderFlow.UpdatedAt,
                //OrderNumber = createdOrderFlow.Order.Id.ToString(),
                //ProjectName = createdOrderFlow.Order.Project.Name,
                //BatchName = createdOrderFlow.Order.Batch.Name
            };

            _logger.LogInformation("Order flow created successfully with ID: {OrderFlowId}", orderFlow.Id);
            return Result<OrderFlowResponse>.Created(response, "Order flow created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order flow for order: {OrderId}", request.OrderId);
            return Result<OrderFlowResponse>.Error("An error occurred while creating the order flow.");
        }
    }

    public async Task<Result<OrderFlowResponse>> UpdateAsync(int id, UpdateOrderFlowRequest request, int currentUserId)
    {
        try
        {
            var orderFlow = await _context.Set<OrderFlow>()
                .Include(of => of.Order)
                    .ThenInclude(o => o.Project)
                .Include(of => of.Order)
                    .ThenInclude(o => o.Batch)
                .FirstOrDefaultAsync(of => of.Id == id);

            if (orderFlow == null)
            {
                _logger.LogWarning("Order flow with ID {OrderFlowId} not found for update", id);
                return Result<OrderFlowResponse>.NotFound();
            }

            // Validate that the order status exists in IAM
            var orderStatusExists = await _context.OrderStatuses
                .AnyAsync(os => os.Id == request.OrderStatusId && os.IsActive);

            if (!orderStatusExists)
            {
                _logger.LogWarning("Attempted to update order flow with non-existent or inactive order status: {OrderStatusId}", request.OrderStatusId);

                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderStatusId),
                    ErrorMessage = $"Order status with ID {request.OrderStatusId} not found or inactive."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if another order flow with the same rank already exists for this order (excluding current)
            var existingOrderFlow = await _context.Set<OrderFlow>()
                .FirstOrDefaultAsync(of => of.OrderId == orderFlow.OrderId && of.Rank == request.Rank && of.Id != id);

            if (existingOrderFlow != null)
            {
                _logger.LogWarning("Attempted to update order flow {OrderFlowId} with existing rank {Rank} for order {OrderId}", id, request.Rank, orderFlow.OrderId);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Rank),
                    ErrorMessage = $"Order flow with rank {request.Rank} already exists for this order."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Update the order flow properties
            orderFlow.OrderStatusId = request.OrderStatusId;
            orderFlow.Rank = request.Rank;
            orderFlow.UpdatedBy = currentUserId;
            orderFlow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get the order status name from IAM
            var orderStatus = await _context.OrderStatuses
                .FirstAsync(os => os.Id == orderFlow.OrderStatusId);

            var response = new OrderFlowResponse
            {
                Id = orderFlow.Id,
                OrderId = orderFlow.OrderId,
                OrderStatusId = orderFlow.OrderStatusId,
                StatusName = orderStatus.Name,
                Rank = orderFlow.Rank,
                CreatedBy = orderFlow.CreatedBy,
                UpdatedBy = orderFlow.UpdatedBy,
                CreatedAt = orderFlow.CreatedAt,
                UpdatedAt = orderFlow.UpdatedAt,
                //OrderNumber = orderFlow.Order.Id.ToString(),
                //ProjectName = orderFlow.Order.Project.Name,
                //BatchName = orderFlow.Order.Batch.Name
            };

            _logger.LogInformation("Order flow updated successfully with ID: {OrderFlowId}", id);
            return Result<OrderFlowResponse>.Success(response, "Order flow updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order flow with ID: {OrderFlowId}", id);
            return Result<OrderFlowResponse>.Error("An error occurred while updating the order flow.");
        }
    }

    public async Task<Result<OrderFlowResponse>> GetByIdAsync(int id)
    {
        try
        {
            var orderFlow = await _context.Set<OrderFlow>()
                .Include(of => of.Order)
                    .ThenInclude(o => o.Project)
                .Include(of => of.Order)
                    .ThenInclude(o => o.Batch)
                .FirstOrDefaultAsync(of => of.Id == id);

            if (orderFlow == null)
            {
                _logger.LogWarning("Order flow with ID {OrderFlowId} not found", id);
                return Result<OrderFlowResponse>.NotFound();
            }

            // Get the order status name from IAM
            var orderStatus = await _context.OrderStatuses
                .FirstAsync(os => os.Id == orderFlow.OrderStatusId);

            var response = new OrderFlowResponse
            {
                Id = orderFlow.Id,
                OrderId = orderFlow.OrderId,
                OrderStatusId = orderFlow.OrderStatusId,
                StatusName = orderStatus.Name,
                Rank = orderFlow.Rank,
                CreatedBy = orderFlow.CreatedBy,
                UpdatedBy = orderFlow.UpdatedBy,
                CreatedAt = orderFlow.CreatedAt,
                UpdatedAt = orderFlow.UpdatedAt,
                //OrderNumber = orderFlow.Order.Id.ToString(),
                //ProjectName = orderFlow.Order.Project.Name,
                //BatchName = orderFlow.Order.Batch.Name
            };

            _logger.LogInformation("Retrieved order flow with ID: {OrderFlowId}", id);
            return Result<OrderFlowResponse>.Success(response, "Order flow retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order flow with ID: {OrderFlowId}", id);
            return Result<OrderFlowResponse>.Error("An error occurred while retrieving the order flow.");
        }
    }
}
