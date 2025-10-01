using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderFlow;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class OrderFlowService : IOrderFlowService
{
    private readonly FluidDbContext _context;
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<OrderFlowService> _logger;

    public OrderFlowService(FluidDbContext context, FluidIAMDbContext iamContext, ILogger<OrderFlowService> logger)
    {
        _context = context;
        _iamContext = iamContext;
        _logger = logger;
    }

    public async Task<Result<OrderFlowResponse>> CreateAsync(CreateOrderFlowRequest request, int currentUserId)
    {
        throw new NotSupportedException("Single-step order flow creation is no longer supported. Use CreateFlowAsync instead.");
    }

    public async Task<Result<OrderFlowResponse>> UpdateAsync(int id, UpdateOrderFlowRequest request, int currentUserId)
    {
        try
        {
            var orderFlow = await _context.Set<OrderFlow>()
                .FirstOrDefaultAsync(of => of.Id == id);

            if (orderFlow == null)
            {
                _logger.LogWarning("Order flow with ID {OrderFlowId} not found for update", id);
                return Result<OrderFlowResponse>.NotFound();
            }

            // Validate that the order status exists in IAM
            var orderStatusExists = await _iamContext.OrderStatuses
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

            // Check if another order flow with the same rank already exists (excluding current)
            var existingOrderFlow = await _context.Set<OrderFlow>()
                .FirstOrDefaultAsync(of => of.Rank == request.Rank && of.Id != id);

            if (existingOrderFlow != null)
            {
                _logger.LogWarning("Attempted to update order flow {OrderFlowId} with existing rank {Rank}", id, request.Rank);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Rank),
                    ErrorMessage = $"Order flow with rank {request.Rank} already exists."
                };

                return Result<OrderFlowResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Update the order flow properties
            orderFlow.OrderStatusId = request.OrderStatusId;
            orderFlow.Rank = request.Rank;
            orderFlow.IsActive = request.IsActive;
            orderFlow.UpdatedBy = currentUserId;
            orderFlow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get the order status name from IAM
            var orderStatus = await _iamContext.OrderStatuses
                .FirstAsync(os => os.Id == orderFlow.OrderStatusId);

            var response = new OrderFlowResponse
            {
                Id = orderFlow.Id,
                OrderStatusId = orderFlow.OrderStatusId,
                StatusName = orderStatus.Name,
                Rank = orderFlow.Rank,
                IsActive = orderFlow.IsActive,
                CreatedBy = orderFlow.CreatedBy,
                UpdatedBy = orderFlow.UpdatedBy,
                CreatedAt = orderFlow.CreatedAt,
                UpdatedAt = orderFlow.UpdatedAt
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
                .FirstOrDefaultAsync(of => of.Id == id);

            if (orderFlow == null)
            {
                _logger.LogWarning("Order flow with ID {OrderFlowId} not found", id);
                return Result<OrderFlowResponse>.NotFound();
            }

            // Get the order status name from IAM
            var orderStatus = await _iamContext.OrderStatuses
                .FirstAsync(os => os.Id == orderFlow.OrderStatusId);

            var response = new OrderFlowResponse
            {
                Id = orderFlow.Id,
                OrderStatusId = orderFlow.OrderStatusId,
                StatusName = orderStatus.Name,
                Rank = orderFlow.Rank,
                IsActive = orderFlow.IsActive,
                CreatedBy = orderFlow.CreatedBy,
                UpdatedBy = orderFlow.UpdatedBy,
                CreatedAt = orderFlow.CreatedAt,
                UpdatedAt = orderFlow.UpdatedAt
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

    public async Task<Result<List<OrderFlowResponse>>> CreateFlowAsync(List<CreateOrderFlowStepRequest> steps, int createdBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var responses = new List<OrderFlowResponse>();
        try
        {
            // Validate all order statuses exist and are active
            var statusIds = steps.Select(s => s.OrderStatusId).Distinct().ToList();
            var validStatuses = await _iamContext.OrderStatuses
                .Where(os => statusIds.Contains(os.Id) && os.IsActive)
                .Select(os => os.Id)
                .ToListAsync();
            var missingStatuses = statusIds.Except(validStatuses).ToList();
            if (missingStatuses.Any())
            {
                return Result<List<OrderFlowResponse>>.Invalid(missingStatuses.Select(ms => new ValidationError {
                    Key = nameof(CreateOrderFlowStepRequest.OrderStatusId),
                    ErrorMessage = $"Order status with ID {ms} not found or inactive."
                }).ToList());
            }

            // Check for duplicate ranks in request
            var duplicateRanks = steps.GroupBy(s => s.Rank).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateRanks.Any())
            {
                return Result<List<OrderFlowResponse>>.Invalid(duplicateRanks.Select(dr => new ValidationError {
                    Key = nameof(CreateOrderFlowStepRequest.Rank),
                    ErrorMessage = $"Duplicate rank {dr} in steps."
                }).ToList());
            }

            // Fetch existing order flows for the tenant
            var existingFlows = await _context.Set<OrderFlow>().ToListAsync();
            var now = DateTime.UtcNow;

            if (existingFlows.Count == 0)
            {
                // CREATE: No existing flows, insert all
                var orderFlows = steps.Select(s => new OrderFlow
                {
                    OrderStatusId = s.OrderStatusId,
                    Rank = s.Rank,
                    IsActive = s.IsActive,
                    CreatedBy = createdBy,
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList();
                _context.Set<OrderFlow>().AddRange(orderFlows);
                await _context.SaveChangesAsync();
                existingFlows = orderFlows;
            }
            else
            {
                // UPDATE: Upsert logic
                // 1. Update existing, 2. Add new, 3. Remove missing
                var stepDict = steps.ToDictionary(s => s.OrderStatusId);
                var flowsToRemove = existingFlows.Where(f => !stepDict.ContainsKey(f.OrderStatusId)).ToList();
                if (flowsToRemove.Any())
                {
                    _context.Set<OrderFlow>().RemoveRange(flowsToRemove);
                }
                foreach (var flow in existingFlows)
                {
                    if (stepDict.TryGetValue(flow.OrderStatusId, out var step))
                    {
                        flow.Rank = step.Rank;
                        flow.IsActive = step.IsActive;
                        flow.UpdatedBy = createdBy;
                        flow.UpdatedAt = now;
                    }
                }
                var existingStatusIds = existingFlows.Select(f => f.OrderStatusId).ToHashSet();
                var newSteps = steps.Where(s => !existingStatusIds.Contains(s.OrderStatusId)).ToList();
                foreach (var step in newSteps)
                {
                    var newFlow = new OrderFlow
                    {
                        OrderStatusId = step.OrderStatusId,
                        Rank = step.Rank,
                        IsActive = step.IsActive,
                        CreatedBy = createdBy,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.Set<OrderFlow>().Add(newFlow);
                    existingFlows.Add(newFlow);
                }
                await _context.SaveChangesAsync();
            }

            // Prepare response
            var statusDict = await _iamContext.OrderStatuses
                .Where(s => existingFlows.Select(f => f.OrderStatusId).Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);
            responses = existingFlows.Select(f => new OrderFlowResponse
            {
                Id = f.Id,
                OrderStatusId = f.OrderStatusId,
                StatusName = statusDict.TryGetValue(f.OrderStatusId, out var name) ? name : string.Empty,
                Rank = f.Rank,
                IsActive = f.IsActive,
                CreatedBy = f.CreatedBy,
                UpdatedBy = f.UpdatedBy,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).OrderBy(f => f.Rank).ToList();

            await transaction.CommitAsync();
            return Result<List<OrderFlowResponse>>.Success(responses, "Order flow steps upserted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error upserting order flow steps");
            return Result<List<OrderFlowResponse>>.Error("An error occurred while upserting the order flow steps.");
        }
    }

    public async Task<Result<List<OrderFlowResponse>>> GetAllAsync()
    {
        try
        {
            var flows = await _context.Set<OrderFlow>().ToListAsync();
            if (flows == null || flows.Count == 0)
                return Result<List<OrderFlowResponse>>.Success(new List<OrderFlowResponse>(), "No order flows found");

            // Get status names in batch
            var statusIds = flows.Select(f => f.OrderStatusId).Distinct().ToList();
            var statusDict = await _iamContext.OrderStatuses
                .Where(s => statusIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            var responses = flows.Select(f => new OrderFlowResponse
            {
                Id = f.Id,
                OrderStatusId = f.OrderStatusId,
                StatusName = statusDict.TryGetValue(f.OrderStatusId, out var name) ? name : string.Empty,
                Rank = f.Rank,
                IsActive = f.IsActive,
                CreatedBy = f.CreatedBy,
                UpdatedBy = f.UpdatedBy,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).OrderBy(f => f.Rank).ToList();

            return Result<List<OrderFlowResponse>>.Success(responses, "Order flows retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order flows");
            return Result<List<OrderFlowResponse>>.Error("An error occurred while retrieving order flows.");
        }
    }
}
