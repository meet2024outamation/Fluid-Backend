using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
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
    private readonly IConfiguration _configuration;

    public OrderService(FluidDbContext context, FluidIAMDbContext iamContext, ILogger<OrderService> logger, IConfiguration configuration)
    {
        _context = context;
        _iamContext = iamContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<OrderListPagedResponse>> GetOrdersAsync(OrderListRequest request)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.Batch)
                .Include(o => o.Project)
                .Include(o => o.Documents)
                .Include(o => o.OrderData)
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

            if (!string.IsNullOrEmpty(request.OrderIdentifier))
            {
                query = query.Where(o => EF.Functions.ILike(o.OrderIdentifier, request.OrderIdentifier));
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

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(o =>
                    EF.Functions.ILike(o.OrderIdentifier, $"%{request.SearchTerm}%") ||
                    o.Documents.Any(d => EF.Functions.ILike(d.Name, $"%{request.SearchTerm}%")) ||
                    o.Documents.Any(d => EF.Functions.ILike(d.SearchableText ?? "", $"%{request.SearchTerm}%")));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = request.SortBy?.ToLowerInvariant() switch
            {
                "orderidentifier" => request.SortDirection?.ToUpperInvariant() == "ASC"
                    ? query.OrderBy(o => o.OrderIdentifier)
                    : query.OrderByDescending(o => o.OrderIdentifier),
                "priority" => request.SortDirection?.ToUpperInvariant() == "ASC"
                    ? query.OrderBy(o => o.Priority)
                    : query.OrderByDescending(o => o.Priority),
                "assignedat" => request.SortDirection?.ToUpperInvariant() == "ASC"
                    ? query.OrderBy(o => o.AssignedAt)
                    : query.OrderByDescending(o => o.AssignedAt),
                "completedat" => request.SortDirection?.ToUpperInvariant() == "ASC"
                    ? query.OrderBy(o => o.CompletedAt)
                    : query.OrderByDescending(o => o.CompletedAt),
                _ => request.SortDirection?.ToUpperInvariant() == "ASC"
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt)
            };

            // Apply pagination
            var orders = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Get order status names
            var orderStatusIds = orders.Select(o => o.OrderStatusId).Distinct().ToList();
            var orderStatuses = await _iamContext.OrderStatuses
                .Where(os => orderStatusIds.Contains(os.Id))
                .ToDictionaryAsync(os => os.Id, os => os.Name ?? "Unknown");
            var users = await _iamContext.Users.ToDictionaryAsync(u => u.Id, u => u.Name ?? "Unknown");
            // Map to response
            var orderResponses = orders.Select(o => new OrderListResponse
            {
                Id = o.Id,
                OrderIdentifier = o.OrderIdentifier,
                BatchId = o.BatchId,
                BatchFileName = o.Batch?.FileName ?? "",
                ProjectId = o.ProjectId,
                ProjectName = o.Project?.Name ?? "",
                Status = orderStatuses.TryGetValue(o.OrderStatusId, out var statusName) ? statusName : "Unknown",
                Priority = o.Priority,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedTo.HasValue && users.TryGetValue(o.AssignedTo.Value, out var assignedToName)
                                ? assignedToName
                                : null,
                AssignedAt = o.AssignedAt,
                StartedAt = o.StartedAt,
                CompletedAt = o.CompletedAt,
                HasValidationErrors = !string.IsNullOrEmpty(o.ValidationErrors),
                DocumentCount = o.Documents?.Count ?? 0,
                FieldCount = o.OrderData?.Count ?? 0,
                VerifiedFieldCount = o.OrderData?.Count(od => od.IsVerified) ?? 0,
                CompletionPercentage = o.OrderData?.Any() == true
                    ? (decimal)o.OrderData.Count(od => od.IsVerified) / o.OrderData.Count * 100
                    : 0,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();

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

            _logger.LogInformation("Retrieved {Count} orders (page {PageNumber} of {TotalPages})",
                orderResponses.Count, request.PageNumber, totalPages);

            return Result<OrderListPagedResponse>.Success(response,
                $"Retrieved {orderResponses.Count} orders successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return Result<OrderListPagedResponse>.Error("An error occurred while retrieving orders.");
        }
    }


    public async Task<Result<OrderDto>> GetOrderByIdAsync(int orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Batch)
                .Include(o => o.Project)
                .Include(o => o.Documents)
                .Include(o => o.OrderData)
                .ThenInclude(od => od.SchemaField)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                return Result<OrderDto>.NotFound();
            }

            // Get order status name
            var orderStatus = await _iamContext.OrderStatuses
                .FirstOrDefaultAsync(os => os.Id == order.OrderStatusId);

            // Get validation errors
            var validationErrors = new List<string>();
            if (!string.IsNullOrEmpty(order.ValidationErrors))
            {
                try
                {
                    validationErrors = JsonSerializer.Deserialize<List<string>>(order.ValidationErrors) ?? new List<string>();
                }
                catch
                {
                    validationErrors.Add(order.ValidationErrors);
                }
            }

            // Map documents to response
            var documentResponses = order.Documents?.Select(d => new DocumentResponse
            {
                Id = d.Id,
                Name = d.Name,
                Type = d.Type,
                Url = d.Url,
                BlobName = d.BlobName,
                SearchableUrl = d.SearchableUrl,
                SearchableBlobName = d.SearchableBlobName,
                Pages = d.Pages,
                HasSearchableText = !string.IsNullOrEmpty(d.SearchableText),
                FileSizeBytes = d.FileSize,
                FileSizeFormatted = FormatFileSize(d.FileSize),
                CreatedAt = d.CreatedAt
            }).ToList() ?? new List<DocumentResponse>();
            var users = await _iamContext.Users.ToDictionaryAsync(u => u.Id, u => u.Name ?? "Unknown");

            var response = new OrderDto
            {
                Id = order.Id,
                OrderIdentifier = order.OrderIdentifier,
                BatchId = order.BatchId,
                BatchName = order.Batch?.Name ?? "",
                ProjectId = order.ProjectId,
                ProjectName = order.Project?.Name ?? "",
                Status = orderStatus?.Name ?? "Unknown",
                Priority = order.Priority,
                AssignedTo = order.AssignedTo,
                AssignedAt = order.AssignedAt,
                AssignedUserName = order.AssignedTo.HasValue && users.TryGetValue(order.AssignedTo.Value, out var assignedToName)
                ? assignedToName
                : null,
                StartedAt = order.StartedAt,
                CompletedAt = order.CompletedAt,
                HasValidationErrors = validationErrors.Any(),
                ValidationErrors = validationErrors,
                DocumentCount = order.Documents?.Count ?? 0,
                FieldCount = order.OrderData?.Count ?? 0,
                VerifiedFieldCount = order.OrderData?.Count(od => od.IsVerified) ?? 0,
                CompletionPercentage = order.OrderData?.Any() == true
                    ? (decimal)order.OrderData.Count(od => od.IsVerified) / order.OrderData.Count * 100
                    : 0,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Documents = documentResponses
            };

            _logger.LogInformation("Retrieved order with ID: {OrderId}", orderId);
            return Result<OrderDto>.Success(response, "Order retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with ID: {OrderId}", orderId);
            return Result<OrderDto>.Error("An error occurred while retrieving the order.");
        }
    }

    // Helper method to format file size
    private static string? FormatFileSize(long? fileSizeBytes)
    {
        if (!fileSizeBytes.HasValue || fileSizeBytes.Value == 0)
            return null;

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = fileSizeBytes.Value;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

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
            var assignedStatusName = GetOrderStatusName(OrderStatus.KeyingInProgress);
            var assignedStatus = await _iamContext.OrderStatuses
                .FirstOrDefaultAsync(os => os.Name == assignedStatusName && os.IsActive);

            if (assignedStatus == null)
            {
                var validationError = new ValidationError
                {
                    Key = "OrderStatus",
                    ErrorMessage = "KeyingInProgress status not found or inactive."
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


    public async Task<Result<UpdateSchemaFieldValueResponse>> UpdateSchemaFieldValueAsync(
        UpdateSchemaFieldValueRequest request, int currentUserId, string? ipAddress, string? userAgent)
    {
        try
        {
            var orderData = await _context.OrderData
                .Include(od => od.Order)
                .Include(od => od.SchemaField)
                .FirstOrDefaultAsync(od => od.Id == request.OrderDataId);

            if (orderData == null)
            {
                return Result<UpdateSchemaFieldValueResponse>.NotFound();
            }

            var oldValue = orderData.ProcessedValue ?? orderData.MetaDataValue ?? "";

            // Update the order data
            orderData.ProcessedValue = request.NewValue;
            orderData.IsVerified = true;
            orderData.VerifiedBy = currentUserId;
            orderData.VerifiedAt = DateTime.UtcNow;
            orderData.UpdatedAt = DateTime.UtcNow;
            orderData.PageNumber = request.PageNumber;
            orderData.Coordinates = request.Coordinates;

            await _context.SaveChangesAsync();

            // Create audit log
            var auditLog = new AuditLog
            {
                TableName = "OrderData",
                RecordId = orderData.Id,
                Action = AuditAction.UPDATE,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { ProcessedValue = oldValue }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { ProcessedValue = request.NewValue }),
                ChangedBy = currentUserId,
                ChangedAt = DateTime.UtcNow,
                IpAddress = ipAddress?[..Math.Min(45, ipAddress.Length)], // Truncate to fit the column
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            var response = new UpdateSchemaFieldValueResponse
            {
                OrderDataId = orderData.Id,
                OrderId = orderData.OrderId,
                SchemaFieldName = orderData.SchemaField?.FieldName ?? "",
                OldValue = oldValue,
                NewValue = request.NewValue,
                UpdatedAt = orderData.UpdatedAt,
                Reason = request.Reason ?? "",
                IsVerified = orderData.IsVerified,
                Message = "Schema field value updated successfully"
            };

            _logger.LogInformation("Schema field value updated for OrderData {OrderDataId} by user {UserId}",
                request.OrderDataId, currentUserId);

            return Result<UpdateSchemaFieldValueResponse>.Success(response, "Schema field value updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema field value for OrderData {OrderDataId}", request.OrderDataId);
            return Result<UpdateSchemaFieldValueResponse>.Error("An error occurred while updating the schema field value.");
        }
    }

    private static string GetOrderStatusName(OrderStatus statusEnum)
        => statusEnum.ToString();
}