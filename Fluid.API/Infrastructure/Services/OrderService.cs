using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;
using Fluid.Entities.Context;
using Fluid.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using System.Text.Json;

namespace Fluid.API.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly FluidDbContext _context;
    private readonly FluidIAMDbContext _iamContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(FluidDbContext context, FluidIAMDbContext iAMDbContext, ILogger<OrderService> logger)
    {
        _context = context;
        _iamContext = iAMDbContext;
        _logger = logger;
    }

    private static string GetOrderStatusName(OrderStatus statusEnum)
        => statusEnum.ToString();

    public async Task<Result<AssignOrderResponse>> AssignOrderAsync(AssignOrderRequest request, int currentUserId)
    {
        try
        {
            // Validate order exists
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderId),
                    ErrorMessage = "Order not found."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate user exists
            var user = await _iamContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.UserId),
                    ErrorMessage = "User not found."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.UserId),
                    ErrorMessage = "Cannot assign to inactive user."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Use enum for assignable statuses
            var assignableEnums = new[]
            {
                OrderStatus.Created,
                OrderStatus.ReadyForAI,
                OrderStatus.ReadyForAssignment
            };
            var assignableStatusNames = assignableEnums.Select(GetOrderStatusName).ToList();
            var assignableStatusIds = await _iamContext.OrderStatuses
                .Where(os => assignableStatusNames.Contains(os.Name) && os.IsActive)
                .Select(os => os.Id)
                .ToListAsync();

            if (!assignableStatusIds.Contains(order.OrderStatusId))
            {
                var currentStatus = (await _iamContext.OrderStatuses.FirstOrDefaultAsync(os => os.Id == order.OrderStatusId))?.Name ?? "Unknown";
                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderId),
                    ErrorMessage = $"Order cannot be assigned in current status: {currentStatus}"
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Get the "Assigned" status using enum
            var assignedStatusName = GetOrderStatusName(OrderStatus.Assigned);
            var assignedStatus = await _iamContext.OrderStatuses
                .FirstOrDefaultAsync(os => os.Name == assignedStatusName && os.IsActive);

            if (assignedStatus == null)
            {
                var validationError = new ValidationError
                {
                    Key = "OrderStatus",
                    ErrorMessage = "Assigned status not found or inactive."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Assign the order
            order.AssignedTo = request.UserId;
            order.AssignedAt = DateTime.UtcNow;
            order.OrderStatusId = assignedStatus.Id;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new AssignOrderResponse
            {
                OrderId = order.Id,
                UserId = user.Id,
                UserName = user.Name,
                AssignedAt = order.AssignedAt.Value,
                Status = assignedStatus.Name,
                Message = "Order assigned successfully"
            };

            _logger.LogInformation("Order {OrderId} assigned to user {UserId} ({UserName})",
                order.Id, user.Id, user.Name);

            return Result<AssignOrderResponse>.Success(response, "Order assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning order {OrderId} to user {UserId}",
                request.OrderId, request.UserId);
            return Result<AssignOrderResponse>.Error("An error occurred while assigning the order.");
        }
    }

    public async Task<Result<UpdateSchemaFieldValueResponse>> UpdateSchemaFieldValueAsync(UpdateSchemaFieldValueRequest request, int currentUserId, string? ipAddress = null, string? userAgent = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate OrderData exists with all related entities
            var orderData = await _context.OrderData
                .Include(od => od.Order)
                .Include(od => od.SchemaField)
                .FirstOrDefaultAsync(od => od.Id == request.OrderDataId);

            if (orderData == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderDataId),
                    ErrorMessage = "Order data record not found."
                };
                return Result<UpdateSchemaFieldValueResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate current user exists
            var currentUser = await _iamContext.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                var validationError = new ValidationError
                {
                    Key = "CurrentUser",
                    ErrorMessage = "Current user not found."
                };
                return Result<UpdateSchemaFieldValueResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Store old values for audit logging
            var oldValues = new
            {
                ProcessedValue = orderData.ProcessedValue,
                IsVerified = orderData.IsVerified,
                VerifiedBy = orderData.VerifiedBy,
                VerifiedAt = orderData.VerifiedAt,
                PageNumber = orderData.PageNumber,
                Coordinates = orderData.Coordinates,
                UpdatedAt = orderData.UpdatedAt
            };

            // Update the field value
            var oldProcessedValue = orderData.ProcessedValue ?? string.Empty;
            orderData.ProcessedValue = request.NewValue;
            orderData.IsVerified = true;
            orderData.VerifiedBy = currentUserId;
            orderData.VerifiedAt = DateTime.UtcNow;
            orderData.UpdatedAt = DateTime.UtcNow;

            // Update page number and coordinates if provided
            if (request.PageNumber.HasValue)
            {
                orderData.PageNumber = request.PageNumber.Value;
            }

            if (!string.IsNullOrEmpty(request.Coordinates))
            {
                orderData.Coordinates = request.Coordinates;
            }

            // Prepare new values for audit logging
            var newValues = new
            {
                ProcessedValue = orderData.ProcessedValue,
                IsVerified = orderData.IsVerified,
                VerifiedBy = orderData.VerifiedBy,
                VerifiedAt = orderData.VerifiedAt,
                PageNumber = orderData.PageNumber,
                Coordinates = orderData.Coordinates,
                UpdatedAt = orderData.UpdatedAt,
                Reason = request.Reason ?? "Manual field update"
            };

            // Save the updated order data
            await _context.SaveChangesAsync();

            // Create audit log entry
            await CreateAuditLogAsync(
                tableName: "OrderData",
                recordId: orderData.Id,
                action: AuditAction.UPDATE,
                oldValues: oldValues,
                newValues: newValues,
                changedBy: currentUserId,
                ipAddress: ipAddress,
                userAgent: userAgent
            );

            await transaction.CommitAsync();

            // Prepare response
            var response = new UpdateSchemaFieldValueResponse
            {
                OrderDataId = orderData.Id,
                OrderId = orderData.OrderId,
                SchemaFieldName = orderData.SchemaField.FieldName,
                OldValue = oldProcessedValue,
                NewValue = orderData.ProcessedValue,
                UpdatedByName = currentUser.Name,
                UpdatedAt = orderData.UpdatedAt,
                Reason = request.Reason ?? "Manual field update",
                IsVerified = orderData.IsVerified,
                Message = "Schema field value updated successfully"
            };

            _logger.LogInformation("Schema field value updated successfully. OrderDataId: {OrderDataId}, OrderId: {OrderId}, Field: {FieldName}, OldValue: '{OldValue}', NewValue: '{NewValue}', UpdatedBy: {UserId} ({UserName})",
                orderData.Id, orderData.OrderId, orderData.SchemaField.FieldName, oldProcessedValue, request.NewValue, currentUserId, currentUser.Name);

            return Result<UpdateSchemaFieldValueResponse>.Success(response, "Schema field value updated successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating schema field value for OrderDataId: {OrderDataId}", request.OrderDataId);
            return Result<UpdateSchemaFieldValueResponse>.Error("An error occurred while updating the schema field value.");
        }
    }

    /// <summary>
    /// Creates an audit log entry for tracking changes
    /// </summary>
    private async Task CreateAuditLogAsync(string tableName, int recordId, AuditAction action, object? oldValues, object? newValues, int changedBy, string? ipAddress, string? userAgent)
    {
        try
        {
            var auditLog = new Entities.Entities.AuditLog
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) : null,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created for {Action} on {TableName} record {RecordId} by user {UserId}",
                action, tableName, recordId, changedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for {Action} on {TableName} record {RecordId}",
                action, tableName, recordId);
            // Don't throw - audit log failures shouldn't break the main operation
        }
    }

    public async Task<Result<OrderListPagedResponse>> GetOrdersAsync(OrderListRequest request, int currentUserId)
    {
        try
        {
            // Build the base query
            var query = _context.Orders
                .Include(o => o.Batch)
                .Include(o => o.Project)
                .Include(o => o.Documents)
                .Include(o => o.OrderData)
                .ThenInclude(od => od.SchemaField)
                .AsQueryable();

            // Apply filters
            if (request.ProjectId.HasValue)
            {
                query = query.Where(o => o.ProjectId == request.ProjectId.Value);
            }

            if (request.BatchId.HasValue)
            {
                query = query.Where(o => o.BatchId == request.BatchId.Value);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<OrderStatus>(request.Status, true, out var statusEnum))
                {
                    var statusName = GetOrderStatusName(statusEnum);
                    var statusId = await _iamContext.OrderStatuses.Where(os => os.Name == statusName).Select(os => os.Id).FirstOrDefaultAsync();
                    query = query.Where(o => o.OrderStatusId == statusId);
                }
                else
                {
                    var statusId = await _iamContext.OrderStatuses.Where(os => os.Name == request.Status).Select(os => os.Id).FirstOrDefaultAsync();
                    query = query.Where(o => o.OrderStatusId == statusId);
                }
            }

            if (request.AssignedTo.HasValue)
            {
                query = query.Where(o => o.AssignedTo == request.AssignedTo.Value);
            }

            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= request.CreatedTo.Value);
            }

            if (request.Priority > 0)
            {
                query = query.Where(o => o.Priority >= request.Priority);
            }

            if (request.HasValidationErrors.HasValue)
            {
                if (request.HasValidationErrors.Value)
                {
                    query = query.Where(o => !string.IsNullOrEmpty(o.ValidationErrors));
                }
                else
                {
                    query = query.Where(o => string.IsNullOrEmpty(o.ValidationErrors));
                }
            }

            // Search functionality
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(o =>
                    o.Documents.Any(d => d.SearchableText != null && d.SearchableText.ToLower().Contains(searchTerm)) ||
                    o.OrderData.Any(od => od.ProcessedValue != null && od.ProcessedValue.ToLower().Contains(searchTerm)) ||
                    o.Batch.FileName.ToLower().Contains(searchTerm) ||
                    o.Project.Name.ToLower().Contains(searchTerm)
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            // Apply pagination
            var skip = (request.PageNumber - 1) * request.PageSize;
            var orders = await query
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync();
            var userIds = orders.Select(o => o.AssignedTo).Where(id => id != null).Distinct().ToList();

            var users = await _iamContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name);
            var orderStatusDict = await _iamContext.OrderStatuses.ToDictionaryAsync(os => os.Id, os => os.Name);
            // Map to response DTOs
            var orderResponses = orders.Select(o => new OrderListResponse
            {
                Id = o.Id,
                BatchId = o.BatchId,
                BatchFileName = o.Batch.FileName,
                ProjectId = o.ProjectId,
                ProjectName = o.Project.Name,
                Status = orderStatusDict.TryGetValue(o.OrderStatusId, out var statusName) ? statusName : "Unknown",
                Priority = o.Priority,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedTo != null && users.ContainsKey(o.AssignedTo.Value)
                                    ? users[o.AssignedTo.Value]
                                    : null,
                AssignedAt = o.AssignedAt,
                StartedAt = o.StartedAt,
                CompletedAt = o.CompletedAt,
                HasValidationErrors = !string.IsNullOrEmpty(o.ValidationErrors),
                DocumentCount = o.Documents.Count,
                FieldCount = o.OrderData.Count,
                VerifiedFieldCount = o.OrderData.Count(od => od.IsVerified),
                CompletionPercentage = o.OrderData.Count > 0
                    ? Math.Round((decimal)o.OrderData.Count(od => od.IsVerified) / o.OrderData.Count * 100, 2)
                    : 0,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var response = new OrderListPagedResponse
            {
                Orders = orderResponses,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1
            };

            _logger.LogInformation("Retrieved {Count} orders (page {PageNumber} of {TotalPages}) for user {UserId}",
                orderResponses.Count, request.PageNumber, totalPages, currentUserId);

            return Result<OrderListPagedResponse>.Success(response, $"Retrieved {orderResponses.Count} orders successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", currentUserId);
            return Result<OrderListPagedResponse>.Error("An error occurred while retrieving orders.");
        }
    }

    /// <summary>
    /// Applies sorting to the orders query
    /// </summary>
    private static IQueryable<Entities.Entities.Order> ApplySorting(IQueryable<Entities.Entities.Order> query, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToUpper() == "DESC";
        // Remove status sorting by navigation property
        return sortBy?.ToLower() switch
        {
            "id" => isDescending ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
            "batchid" => isDescending ? query.OrderByDescending(o => o.BatchId) : query.OrderBy(o => o.BatchId),
            "projectname" => isDescending ? query.OrderByDescending(o => o.Project.Name) : query.OrderBy(o => o.Project.Name),
            // Sorting by status name must be handled after fetching data
            "priority" => isDescending ? query.OrderByDescending(o => o.Priority) : query.OrderBy(o => o.Priority),
            "assignedat" => isDescending ? query.OrderByDescending(o => o.AssignedAt) : query.OrderBy(o => o.AssignedAt),
            "startedat" => isDescending ? query.OrderByDescending(o => o.StartedAt) : query.OrderBy(o => o.StartedAt),
            "completedat" => isDescending ? query.OrderByDescending(o => o.CompletedAt) : query.OrderBy(o => o.CompletedAt),
            "updatedat" => isDescending ? query.OrderByDescending(o => o.UpdatedAt) : query.OrderBy(o => o.UpdatedAt),
            "createdat" or _ => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
        };
    }
}