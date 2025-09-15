using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.AI;

/// <summary>
/// Request model for document AI extraction
/// </summary>
public class DocumentExtractionRequest
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64 encoded file content
    public string ContentType { get; set; } = string.Empty;
    public int WorkItemId { get; set; }
    public ExtractionOptions ProcessingOptions { get; set; } = new();
}

/// <summary>
/// Options for AI extraction processing
/// </summary>
public class ExtractionOptions
{
    public bool ExtractText { get; set; } = true;
    public bool ExtractTables { get; set; } = true;
    public bool ExtractKeyValuePairs { get; set; } = true;
    public bool EnableOCR { get; set; } = true;
    public bool GenerateSearchablePDF { get; set; } = true; // New option for searchable PDF generation
    public string Language { get; set; } = "en";
    public List<string>? TargetFields { get; set; } // Specific fields to extract
}

/// <summary>
/// Response model from AI extraction API
/// </summary>
public class DocumentExtractionResult
{
    public bool IsSuccess { get; set; }
    public string? ExtractedText { get; set; }
    public int PageCount { get; set; }
    public List<ExtractedField> ExtractedFields { get; set; } = new();
    public List<ExtractedTable> ExtractedTables { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public double ProcessingTimeMs { get; set; }

    // New fields for searchable document URLs
    public string? SearchableUrl { get; set; }
    public string? SearchableBlobName { get; set; }

    public static DocumentExtractionResult Success(string extractedText, List<ExtractedField>? fields = null)
    {
        return new DocumentExtractionResult
        {
            IsSuccess = true,
            ExtractedText = extractedText,
            ExtractedFields = fields ?? new List<ExtractedField>()
        };
    }

    public static DocumentExtractionResult Error(string error)
    {
        return new DocumentExtractionResult
        {
            IsSuccess = false,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Represents a key-value pair extracted from document
/// </summary>
public class ExtractedField
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0.0 to 1.0
    public int PageNumber { get; set; }
    public BoundingBox? Location { get; set; }
    public string DataType { get; set; } = "string"; // string, number, date, currency, etc.
}

/// <summary>
/// Represents a table extracted from document
/// </summary>
public class ExtractedTable
{
    public int TableIndex { get; set; }
    public int PageNumber { get; set; }
    public List<List<string>> Rows { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public double Confidence { get; set; }
    public BoundingBox? Location { get; set; }
}

/// <summary>
/// Represents location coordinates in document
/// </summary>
public class BoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

/// <summary>
/// Configuration settings for AI extraction
/// </summary>
public class AIExtractionSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public List<string> SupportedFileTypes { get; set; } = new() { ".pdf", ".png", ".jpg", ".jpeg", ".tiff", ".tif" };
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB default
}