using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Batch;

public class CreateBatchRequest
{
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public int ProjectId { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public IFormFile MetadataFile { get; set; } = null!;

    public IFormFileCollection? Documents { get; set; }
}
public class ProcessingResult
{
    public bool IsSuccess { get; set; }
    public int TotalOrders { get; set; }
    public int ProcessedOrders { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ProcessingResult Success(int total, int processed)
    {
        return new ProcessingResult { IsSuccess = true, TotalOrders = total, ProcessedOrders = processed };
    }

    public static ProcessingResult Error(string error)
    {
        return new ProcessingResult { IsSuccess = false, Errors = new List<string> { error } };
    }
}
public class BatchResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ProcessedOrders { get; set; }
    public int ValidOrders { get; set; }
    public int InvalidOrders { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public List<BatchValidationResult> ValidationResults { get; set; } = new();
}

public class BatchListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ProcessedOrders { get; set; }
    public int ValidOrders { get; set; }
    public int InvalidOrders { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class BatchValidationResult
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int AffectedOrders { get; set; }

    public BatchValidationResult() { }

    public BatchValidationResult(string type, string message, int affectedOrders)
    {
        Type = type;
        Message = message;
        AffectedOrders = affectedOrders;
    }
}

public class UpdateBatchStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class BatchProcessingRequest
{
    [Required]
    public int BatchId { get; set; }
}

public class BatchOrderResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasValidationErrors { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public BatchOrderResponse() { }

    public BatchOrderResponse(int id, string status, bool hasValidationErrors, List<string> validationErrors, DateTime createdAt)
    {
        Id = id;
        Status = status;
        HasValidationErrors = hasValidationErrors;
        ValidationErrors = validationErrors;
        CreatedAt = createdAt;
    }
}

public class ReprocessBatchRequest
{
    [Required]
    public int BatchId { get; set; }
    public bool ReprocessValidationErrors { get; set; } = false;
}