using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Order;

public class AssignOrderRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int UserId { get; set; }
}

public class AssignOrderResponse
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UpdateSchemaFieldValueRequest
{
    [Required]
    public int OrderDataId { get; set; }

    [Required]
    [StringLength(1000)]
    public string NewValue { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; set; }

    public int? PageNumber { get; set; }

    [StringLength(1000)]
    public string? Coordinates { get; set; }
}

public class UpdateSchemaFieldValueResponse
{
    public int OrderDataId { get; set; }
    public int OrderId { get; set; }
    public string SchemaFieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string UpdatedByName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class OrderListRequest
{
    public int? ProjectId { get; set; }
    public int? BatchId { get; set; }
    public string? Status { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Priority { get; set; } = 1; // 1-10 priority filter
    public bool? HasValidationErrors { get; set; }
    public string? SearchTerm { get; set; } // Search in documents or order data
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortDirection { get; set; } = "DESC"; // ASC or DESC
}

public class OrderListResponse
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string BatchFileName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool HasValidationErrors { get; set; }
    public int DocumentCount { get; set; }
    public int FieldCount { get; set; }
    public int VerifiedFieldCount { get; set; }
    public decimal CompletionPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OrderListPagedResponse
{
    public List<OrderListResponse> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}