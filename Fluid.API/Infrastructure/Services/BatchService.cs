using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.AI;
using Fluid.API.Models.Batch;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Fluid.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using System.Text.Json;

namespace Fluid.API.Infrastructure.Services;

internal class ProcessingResult
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

public class BatchService : IBatchService
{
    private readonly FluidDbContext _context;
    private readonly ILogger<BatchService> _logger;
    private readonly IConfiguration _configuration;

    public BatchService(FluidDbContext context, ILogger<BatchService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<BatchResponse>> CreateAsync(CreateBatchRequest request, int currentUserId)
    {
        try
        {
            // Validate project exists
            var project = await _context.Projects
                .FirstOrDefaultAsync(c => c.Id == request.ProjectId);

            if (project == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.ProjectId),
                    ErrorMessage = "Project not found."
                };
                return Result<BatchResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate metadata file
            if (request.MetadataFile == null || request.MetadataFile.Length == 0)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.MetadataFile),
                    ErrorMessage = "Metadata file is required."
                };
                return Result<BatchResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate file type (CSV, Excel, etc.)
            var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(request.MetadataFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.MetadataFile),
                    ErrorMessage = "Only CSV and Excel files are supported for metadata."
                };
                return Result<BatchResponse>.Invalid(new List<ValidationError> { validationError });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            // Save metadata file and get the URL
            var metadataFileUrl = await SaveFileAsync(request.MetadataFile, "metadata", project.Code);

            // Create batch
            var batch = new Entities.Entities.Batch
            {
                FileName = request.FileName,
                ProjectId = request.ProjectId,
                Name = request.Name,
                FileUrl = metadataFileUrl,
                Status = BatchStatus.Received,
                TotalOrders = 0,
                ProcessedOrders = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            // Process metadata file and create orders
            var processingResult = await ProcessMetadataFile(batch, request.MetadataFile, request.Documents);

            if (!processingResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return Result<BatchResponse>.Error(processingResult.Errors.ToArray());
            }

            // Update batch with order counts
            batch.TotalOrders = processingResult.TotalOrders;
            batch.ProcessedOrders = processingResult.ProcessedOrders;
            batch.Status = BatchStatus.Processing;

            await _context.SaveChangesAsync();

            // Perform validations
            var validationResults = await PerformBatchValidations(batch.Id);

            // Update batch status based on validation results
            batch.Status = validationResults.Any(v => v.Type == "Error") ? BatchStatus.Error : BatchStatus.Ready;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Load the created batch with all details
            var createdBatch = await GetBatchWithDetails(batch.Id);
            var response = MapToBatchResponse(createdBatch!, validationResults);

            _logger.LogInformation("Batch created successfully with ID: {BatchId}", batch.Id);
            return Result<BatchResponse>.Created(response, "Batch created and processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch with file: {FileName}", request.FileName);
            return Result<BatchResponse>.Error("An error occurred while creating the batch.");
        }
    }

    public async Task<Result<BatchResponse>> GetByIdAsync(int id)
    {
        try
        {
            var batch = await GetBatchWithDetails(id);

            if (batch == null)
            {
                _logger.LogWarning("Batch with ID {BatchId} not found", id);
                return Result<BatchResponse>.NotFound();
            }

            var validationResults = await GetBatchValidationResults(id);
            var response = MapToBatchResponse(batch, validationResults);

            _logger.LogInformation("Retrieved batch with ID: {BatchId}", id);
            return Result<BatchResponse>.Success(response, "Batch retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch with ID: {BatchId}", id);
            return Result<BatchResponse>.Error("An error occurred while retrieving the batch.");
        }
    }

    public async Task<Result<List<BatchListResponse>>> GetAllAsync()
    {
        try
        {
            var batches = await _context.Batches
                .Include(b => b.Project)
                //.Include(b => b.CreatedByUser)
                .ToListAsync();

            var batchListResponses = batches.Select(b => new BatchListResponse
            {
                Id = b.Id,
                Name = b.Name,
                FileName = b.FileName,
                ProjectName = b.Project.Name,
                Status = b.Status.ToString(),
                TotalOrders = b.TotalOrders,
                ProcessedOrders = b.ProcessedOrders,
                ValidOrders = b.Orders.Count(w => w.Status != OrderStatus.ValidationError),
                InvalidOrders = b.Orders.Count(w => w.Status == OrderStatus.ValidationError),
                CreatedAt = b.CreatedAt,
                //CreatedByName = b.CreatedByUser.Name
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

            _logger.LogInformation("Retrieved {Count} batches", batchListResponses.Count);
            return Result<List<BatchListResponse>>.Success(batchListResponses, $"Retrieved {batchListResponses.Count} batches successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches");
            return Result<List<BatchListResponse>>.Error("An error occurred while retrieving batches.");
        }
    }

    public async Task<Result<List<BatchListResponse>>> GetByProjectIdAsync(int projectId)
    {
        try
        {
            var batches = await _context.Batches
                .Include(b => b.Project)
                //.Include(b => b.CreatedByUser)
                .Where(b => b.ProjectId == projectId)
                .ToListAsync();

            var batchListResponses = batches.Select(b => new BatchListResponse
            {
                Id = b.Id,
                FileName = b.FileName,
                ProjectName = b.Project.Name,
                Status = b.Status.ToString(),
                TotalOrders = b.TotalOrders,
                ProcessedOrders = b.ProcessedOrders,
                ValidOrders = b.Orders.Count(w => w.Status != OrderStatus.ValidationError),
                InvalidOrders = b.Orders.Count(w => w.Status == OrderStatus.ValidationError),
                CreatedAt = b.CreatedAt,
                //CreatedByName = b.CreatedByUser.Name
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

            _logger.LogInformation("Retrieved {Count} batches for project {ProjectId}", batchListResponses.Count, projectId);
            return Result<List<BatchListResponse>>.Success(batchListResponses, $"Retrieved {batchListResponses.Count} batches successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for project {ProjectId}", projectId);
            return Result<List<BatchListResponse>>.Error("An error occurred while retrieving batches.");
        }
    }

    public async Task<Result<BatchResponse>> UpdateStatusAsync(int id, UpdateBatchStatusRequest request, int currentUserId)
    {
        try
        {
            var batch = await _context.Batches
                .Include(b => b.Project)
                //.Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null)
            {
                _logger.LogWarning("Batch with ID {BatchId} not found for status update", id);
                return Result<BatchResponse>.NotFound();
            }

            // Validate status transition
            if (!Enum.TryParse<BatchStatus>(request.Status, out var newStatus))
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.Status),
                    ErrorMessage = "Invalid status value."
                };
                return Result<BatchResponse>.Invalid(new List<ValidationError> { validationError });
            }

            batch.Status = newStatus;
            await _context.SaveChangesAsync();

            var validationResults = await GetBatchValidationResults(id);
            var response = MapToBatchResponse(batch, validationResults);

            _logger.LogInformation("Batch status updated successfully for ID: {BatchId}", id);
            return Result<BatchResponse>.Success(response, "Batch status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating batch status for ID: {BatchId}", id);
            return Result<BatchResponse>.Error("An error occurred while updating the batch status.");
        }
    }

    public async Task<Result<BatchResponse>> ProcessBatchAsync(int id, int currentUserId)
    {
        try
        {
            var batch = await GetBatchWithDetails(id);

            if (batch == null)
            {
                _logger.LogWarning("Batch with ID {BatchId} not found for processing", id);
                return Result<BatchResponse>.NotFound();
            }

            if (batch.Status != BatchStatus.Received)
            {
                var validationError = new ValidationError
                {
                    Key = "Status",
                    ErrorMessage = "Batch can only be processed from 'Received' status."
                };
                return Result<BatchResponse>.Invalid(new List<ValidationError> { validationError });
            }

            batch.Status = BatchStatus.Processing;
            await _context.SaveChangesAsync();

            // Perform processing logic here
            var validationResults = await PerformBatchValidations(id);

            // Update status based on results
            batch.Status = validationResults.Any(v => v.Type == "Error") ? BatchStatus.Error : BatchStatus.Ready;
            await _context.SaveChangesAsync();

            var response = MapToBatchResponse(batch, validationResults);

            _logger.LogInformation("Batch processed successfully for ID: {BatchId}", id);
            return Result<BatchResponse>.Success(response, "Batch processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch with ID: {BatchId}", id);
            return Result<BatchResponse>.Error("An error occurred while processing the batch.");
        }
    }

    public async Task<Result<BatchResponse>> ReprocessBatchAsync(ReprocessBatchRequest request, int currentUserId)
    {
        try
        {
            var batch = await GetBatchWithDetails(request.BatchId);

            if (batch == null)
            {
                _logger.LogWarning("Batch with ID {BatchId} not found for reprocessing", request.BatchId);
                return Result<BatchResponse>.NotFound();
            }

            batch.Status = BatchStatus.Processing;
            await _context.SaveChangesAsync();

            // If reprocessing validation errors, reset order statuses
            if (request.ReprocessValidationErrors)
            {
                var errorOrders = await _context.Orders
                    .Where(w => w.BatchId == request.BatchId && w.Status == OrderStatus.ValidationError)
                    .ToListAsync();

                foreach (var order in errorOrders)
                {
                    order.Status = OrderStatus.Created;
                    order.ValidationErrors = null;
                }
            }

            await _context.SaveChangesAsync();

            // Perform validations again
            var validationResults = await PerformBatchValidations(request.BatchId);

            // Update status
            batch.Status = validationResults.Any(v => v.Type == "Error") ? BatchStatus.Error : BatchStatus.Ready;
            await _context.SaveChangesAsync();

            var response = MapToBatchResponse(batch, validationResults);

            _logger.LogInformation("Batch reprocessed successfully for ID: {BatchId}", request.BatchId);
            return Result<BatchResponse>.Success(response, "Batch reprocessed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing batch with ID: {BatchId}", request.BatchId);
            return Result<BatchResponse>.Error("An error occurred while reprocessing the batch.");
        }
    }

    public async Task<Result<List<BatchOrderResponse>>> GetBatchOrdersAsync(int batchId)
    {
        try
        {
            var workItems = await _context.Orders
                .Where(w => w.BatchId == batchId)
                .ToListAsync();

            var orders = workItems.Select(w => new BatchOrderResponse(
                    w.Id,
                    w.Status.ToString(),
                    !string.IsNullOrEmpty(w.ValidationErrors),
                    string.IsNullOrEmpty(w.ValidationErrors)
                        ? new List<string>()
                        : (JsonSerializer.Deserialize<List<string>>(w.ValidationErrors) ?? new List<string>()),
                    w.CreatedAt
                ))
                .ToList();

            _logger.LogInformation("Retrieved {Count} orders for batch {BatchId}", orders.Count, batchId);
            return Result<List<BatchOrderResponse>>.Success(orders, $"Retrieved {orders.Count} orders successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for batch {BatchId}", batchId);
            return Result<List<BatchOrderResponse>>.Error("An error occurred while retrieving batch orders.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var batch = await _context.Batches
                .Include(b => b.Orders)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null)
            {
                _logger.LogWarning("Batch with ID {BatchId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Remove associated orders first
            _context.Orders.RemoveRange(batch.Orders);

            // Remove the batch
            _context.Batches.Remove(batch);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch deleted successfully with ID: {BatchId}", id);
            return Result<bool>.Success(true, "Batch deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting batch with ID: {BatchId}", id);
            return Result<bool>.Error("An error occurred while deleting the batch.");
        }
    }

    private async Task<Entities.Entities.Batch?> GetBatchWithDetails(int batchId)
    {
        return await _context.Batches
            .Include(b => b.Project)
            //.Include(b => b.CreatedByUser)
            .Include(b => b.Orders)
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }

    private async Task<ProcessingResult> ProcessMetadataFile(Entities.Entities.Batch batch, IFormFile metadataFile, IFormFileCollection? documents)
    {
        var result = new ProcessingResult();

        try
        {
            // Get project information for file storage
            var project = await _context.Projects.FirstOrDefaultAsync(c => c.Id == batch.ProjectId);
            if (project == null)
            {
                return ProcessingResult.Error("Project not found for this batch.");
            }

            // Get project's field mappings and schema fields
            var fieldMappings = await _context.FieldMappings
                .Include(fm => fm.Schema)
                .ThenInclude(s => s.SchemaFields)
                .Where(fm => fm.ProjectId == batch.ProjectId)
                .ToListAsync();

            if (!fieldMappings.Any())
            {
                return ProcessingResult.Error("No field mappings found for this project. Please configure field mappings first.");
            }

            // Create a dictionary for quick lookup of schema field IDs by input field name
            var fieldMappingLookup = new Dictionary<string, int>();
            foreach (var mapping in fieldMappings)
            {
                fieldMappingLookup[mapping.InputField.ToLowerInvariant()] = mapping.SchemaFieldId;
            }

            // Save the uploaded file temporarily
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await metadataFile.CopyToAsync(stream);
            }

            // Parse the file based on extension
            var fileExtension = Path.GetExtension(metadataFile.FileName).ToLowerInvariant();
            List<Dictionary<string, object>> groupedRecords;

            if (fileExtension == ".csv")
            {
                groupedRecords = await ParseCsvFile(tempPath);
            }
            else if (fileExtension == ".xlsx" || fileExtension == ".xls")
            {
                groupedRecords = await ParseExcelFile(tempPath);
            }
            else
            {
                return ProcessingResult.Error("Unsupported file format");
            }

            // Create a dictionary to match documents by filename
            var documentLookup = new Dictionary<string, IFormFile>();
            if (documents != null)
            {
                foreach (var doc in documents)
                {
                    documentLookup[doc.FileName] = doc;
                }
            }

            // Create orders from grouped records
            foreach (var groupedRecord in groupedRecords)
            {
                var order = new Order
                {
                    BatchId = batch.Id,
                    ProjectId = batch.ProjectId,
                    Status = OrderStatus.Created,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get the order ID

                // Create order data from metadata
                if (groupedRecord["metadata"] is Dictionary<string, object> metadata)
                {
                    foreach (var field in metadata)
                    {
                        var inputFieldName = field.Key.ToLowerInvariant();

                        // Skip non-string values and complex objects
                        if (field.Value == null || field.Value is not string stringValue)
                            continue;

                        // Check if this input field has a mapping to a schema field
                        if (fieldMappingLookup.TryGetValue(inputFieldName, out var schemaFieldId))
                        {
                            var orderData = new OrderData
                            {
                                OrderId = order.Id,
                                SchemaFieldId = schemaFieldId,
                                MetaDataValue = stringValue,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.OrderData.Add(orderData);
                        }
                        else
                        {
                            _logger.LogWarning("Input field '{InputField}' has no mapping to schema field for project {ProjectId}",
                                field.Key, batch.ProjectId);
                        }
                    }
                }

                // Create document records from the document information
                if (groupedRecord["documents"] is List<Dictionary<string, object>> documentInfos)
                {
                    foreach (var documentInfo in documentInfos)
                    {
                        var filename = documentInfo.ContainsKey("filename") ? documentInfo["filename"]?.ToString() : null;
                        var documentType = documentInfo.ContainsKey("type") ? documentInfo["type"]?.ToString() : null;

                        if (!string.IsNullOrEmpty(filename))
                        {
                            // Check if we have the actual file uploaded
                            if (documentLookup.TryGetValue(filename, out var documentFile))
                            {
                                // Save the document file
                                var documentUrl = await SaveFileAsync(documentFile, "documents", project.Code);

                                // Create document record with actual file
                                var documentRecord = new Document
                                {
                                    OrderId = order.Id,
                                    Name = filename,
                                    Type = documentType ?? Path.GetExtension(filename),
                                    Url = documentUrl,
                                    FileSize = documentFile.Length,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.Documents.Add(documentRecord);

                                // Process document for AI extraction after saving
                                await _context.SaveChangesAsync();
                                //await ProcessDocumentForExtractionAsync(documentRecord, documentFile);
                            }
                            else
                            {
                                // Create document record without actual file (placeholder)
                                var documentRecord = new Document
                                {
                                    OrderId = order.Id,
                                    Name = filename,
                                    Type = documentType ?? Path.GetExtension(filename),
                                    Url = $"pending/{filename}", // Placeholder URL
                                    FileSize = 0,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.Documents.Add(documentRecord);

                                _logger.LogWarning("Document file '{FileName}' referenced in metadata but not found in uploaded files", filename);
                            }
                        }
                    }
                }

                result.ProcessedOrders++;
            }

            result.TotalOrders = groupedRecords.Count;

            // Save all order data and documents
            await _context.SaveChangesAsync();

            // Clean up temp file
            File.Delete(tempPath);

            return ProcessingResult.Success(result.TotalOrders, result.ProcessedOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing metadata file for batch {BatchId}", batch.Id);
            return ProcessingResult.Error($"Error processing file: {ex.Message}");
        }
    }

    private async Task<List<Dictionary<string, object>>> ParseCsvFile(string filePath)
    {
        var groupedRecords = new List<Dictionary<string, object>>();
        var lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length < 2) return groupedRecords; // Need at least header and one data row

        var headers = lines[0].Split(',').Select(h => h.Trim('"')).ToArray();

        if (headers.Length == 0) return groupedRecords;

        var groupingField = headers[0]; // First field is used for grouping (e.g., loan_id)
        var recordGroups = new Dictionary<string, List<Dictionary<string, string>>>();

        // Parse all rows and group by first field
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',').Select(v => v.Trim('"')).ToArray();
            var record = new Dictionary<string, string>();

            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
            {
                record[headers[j]] = values[j];
            }

            if (record.Count > 0 && !string.IsNullOrEmpty(record[groupingField]))
            {
                var groupKey = record[groupingField];
                if (!recordGroups.ContainsKey(groupKey))
                {
                    recordGroups[groupKey] = new List<Dictionary<string, string>>();
                }
                recordGroups[groupKey].Add(record);
            }
        }

        // Create grouped records with metadata and document information
        foreach (var group in recordGroups)
        {
            var groupKey = group.Key;
            var records = group.Value;
            var firstRecord = records.First();

            // Create metadata from the first record (excluding document-specific fields)
            var metadata = new Dictionary<string, object>();
            var documentSpecificFields = new[] { "document_type", "pdf_filename" }; // Fields that vary per document

            foreach (var field in firstRecord)
            {
                if (!documentSpecificFields.Contains(field.Key.ToLowerInvariant()))
                {
                    metadata[field.Key] = field.Value;
                }
            }

            // Add document count and types to metadata
            var documentTypes = records.Select(r => r.ContainsKey("document_type") ? r["document_type"] : "unknown")
                                     .Where(dt => !string.IsNullOrEmpty(dt))
                                     .Distinct()
                                     .ToList();

            metadata["document_count"] = records.Count;
            metadata["document_types"] = documentTypes;

            // Create document information
            var documents = new List<Dictionary<string, object>>();
            foreach (var record in records)
            {
                var document = new Dictionary<string, object>();

                if (record.ContainsKey("pdf_filename") && !string.IsNullOrEmpty(record["pdf_filename"]))
                {
                    document["filename"] = record["pdf_filename"];
                }

                if (record.ContainsKey("document_type") && !string.IsNullOrEmpty(record["document_type"]))
                {
                    document["type"] = record["document_type"];
                }

                // Add any other document-specific fields
                foreach (var field in record)
                {
                    if (documentSpecificFields.Contains(field.Key.ToLowerInvariant()) &&
                        !document.ContainsKey(field.Key.ToLowerInvariant().Replace("_", "")))
                    {
                        document[field.Key] = field.Value;
                    }
                }

                documents.Add(document);
            }

            // Create the grouped record
            var groupedRecord = new Dictionary<string, object>
            {
                ["grouping_key"] = groupKey,
                ["metadata"] = metadata,
                ["documents"] = documents,
                ["record_count"] = records.Count
            };

            groupedRecords.Add(groupedRecord);
        }

        return groupedRecords;
    }

    private async Task<List<Dictionary<string, object>>> ParseExcelFile(string filePath)
    {
        // Placeholder for Excel parsing - you would use a library like EPPlus or ClosedXML
        // For now, return empty list but with the correct structure
        await Task.CompletedTask;

        // When implementing, follow the same grouping logic as ParseCsvFile:
        // 1. Parse Excel file rows
        // 2. Group by first column value
        // 3. Create metadata from common fields
        // 4. Extract document information
        // 5. Return structured grouped records

        return new List<Dictionary<string, object>>();
    }

    private async Task<List<BatchValidationResult>> PerformBatchValidations(int batchId)
    {
        var validationResults = new List<BatchValidationResult>();

        // Get all orders for this batch with their order data and schema fields
        var orders = await _context.Orders
            .Include(w => w.OrderData)
            .ThenInclude(od => od.SchemaField)
            .Where(w => w.BatchId == batchId)
            .ToListAsync();

        // Get the project ID for this batch to determine required fields
        var batch = await _context.Batches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            validationResults.Add(new BatchValidationResult(
                "Error",
                "Batch not found during validation",
                0
            ));
            return validationResults;
        }

        // Get schema fields marked as required for this project
        var requiredSchemaFields = await _context.SchemaFields
            .Include(sf => sf.Schema)
            .ThenInclude(s => s.ProjectSchemas)
            .Where(sf => sf.Schema.ProjectSchemas.Any(cs => cs.ProjectId == batch.ProjectId) && sf.IsRequired)
            .Select(sf => sf.Id)
            .ToListAsync();

        // Validation 1: Check for missing required fields
        //var ordersWithMissingFields = new List<int>();
        //foreach (var order in orders)
        //{
        //    var orderSchemaFieldIds = order.OrderData.Select(od => od.SchemaFieldId).ToList();

        //    // Check if any required fields are missing
        //    var missingRequiredFields = requiredSchemaFields.Except(orderSchemaFieldIds).ToList();

        //    if (missingRequiredFields.Any())
        //    {
        //        ordersWithMissingFields.Add(order.Id);
        //        order.Status = OrderStatus.ValidationError;

        //        // Get the names of missing fields for error message
        //        var missingFieldNames = await _context.SchemaFields
        //            .Where(sf => missingRequiredFields.Contains(sf.Id))
        //            .Select(sf => sf.FieldName)
        //            .ToListAsync();

        //        order.ValidationErrors = JsonSerializer.Serialize(new[] { $"Missing required fields: {string.Join(", ", missingFieldNames)}" });
        //    }
        //}

        //if (ordersWithMissingFields.Any())
        //{
        //    validationResults.Add(new BatchValidationResult(
        //        "Error",
        //        $"Missing required fields in {ordersWithMissingFields.Count} orders",
        //        ordersWithMissingFields.Count
        //    ));
        //}

        // Validation 2: Check for missing PDF documents
        var ordersWithMissingPDFs = new List<int>();
        foreach (var order in orders)
        {
            var hasDocuments = await _context.Documents
                .AnyAsync(d => d.OrderId == order.Id);

            if (!hasDocuments)
            {
                ordersWithMissingPDFs.Add(order.Id);
                if (order.Status != OrderStatus.ValidationError)
                {
                    order.Status = OrderStatus.ValidationError;
                    var errors = new List<string>();
                    if (!string.IsNullOrEmpty(order.ValidationErrors))
                    {
                        errors = JsonSerializer.Deserialize<List<string>>(order.ValidationErrors) ?? new List<string>();
                    }
                    errors.Add("Missing PDF documents");
                    order.ValidationErrors = JsonSerializer.Serialize(errors);
                }
            }
        }

        if (ordersWithMissingPDFs.Any())
        {
            validationResults.Add(new BatchValidationResult(
                "Warning",
                $"Missing PDF documents in {ordersWithMissingPDFs.Count} orders",
                ordersWithMissingPDFs.Count
            ));
        }

        // Mark valid orders as ready for AI processing
        foreach (var order in orders.Where(o => o.Status == OrderStatus.Created))
        {
            order.Status = OrderStatus.ReadyForAI;
        }

        await _context.SaveChangesAsync();

        return validationResults;
    }

    private async Task<List<BatchValidationResult>> GetBatchValidationResults(int batchId)
    {
        var results = new List<BatchValidationResult>();

        var orders = await _context.Orders
            .Where(w => w.BatchId == batchId)
            .ToListAsync();

        var errorOrders = orders.Where(o => o.Status == OrderStatus.ValidationError).ToList();
        var readyOrders = orders.Where(o => o.Status == OrderStatus.ReadyForAI).ToList();

        if (errorOrders.Any())
        {
            results.Add(new BatchValidationResult(
                "Error",
                $"{errorOrders.Count} orders have validation errors",
                errorOrders.Count
            ));
        }

        if (readyOrders.Any())
        {
            results.Add(new BatchValidationResult(
                "Success",
                $"{readyOrders.Count} orders are ready for processing",
                readyOrders.Count
            ));
        }

        return results;
    }

    private static BatchResponse MapToBatchResponse(Entities.Entities.Batch batch, List<BatchValidationResult> validationResults)
    {
        var validOrders = batch.Orders.Count(w => w.Status != OrderStatus.ValidationError);
        var invalidOrders = batch.Orders.Count(w => w.Status == OrderStatus.ValidationError);

        return new BatchResponse
        {
            Id = batch.Id,
            FileName = batch.FileName,
            ProjectId = batch.ProjectId,
            ProjectName = batch.Project.Name,
            FileUrl = batch.FileUrl,
            Status = batch.Status.ToString(),
            TotalOrders = batch.TotalOrders,
            ProcessedOrders = batch.ProcessedOrders,
            ValidOrders = validOrders,
            InvalidOrders = invalidOrders,
            CreatedAt = batch.CreatedAt,
            CreatedBy = batch.CreatedBy,
            //CreatedByName = batch.CreatedByUser.Name,
            ValidationResults = validationResults
        };
    }

    private async Task<string> SaveFileAsync(IFormFile file, string fileType, string projectCode)
    {
        try
        {
            // Get the uploads directory from configuration or use default
            var uploadsPath = _configuration["FileStorage:UploadsPath"] ?? "uploads";
            var projectPath = Path.Combine(uploadsPath, projectCode, fileType);

            // Ensure directory exists
            Directory.CreateDirectory(projectPath);

            // Generate unique filename
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(projectPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL path
            return Path.Combine(projectCode, fileType, fileName).Replace('\\', '/');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileName} for project {ProjectCode}", file.FileName, projectCode);
            throw;
        }
    }

    /// <summary>
    /// Processes a document for AI extraction to make it searchable
    /// </summary>
    /// <param name="document">The document entity to process</param>
    /// <param name="documentFile">The uploaded file content</param>
    private async Task ProcessDocumentForExtractionAsync(Document document, IFormFile documentFile)
    {
        try
        {
            _logger.LogInformation("Starting AI extraction for document {DocumentId} ({DocumentName})",
                document.Id, document.Name);

            // Step 1: Prepare document for AI API
            var extractionRequest = await PrepareDocumentForAIAsync(document, documentFile);

            // Step 2: Send to AI API for text extraction
            var extractionResult = await SendDocumentToAIAPIAsync(extractionRequest);

            // Step 3: Process and store the extracted text
            if (extractionResult.IsSuccess)
            {
                await UpdateDocumentWithExtractedDataAsync(document, extractionResult);
                _logger.LogInformation("AI extraction completed successfully for document {DocumentId}", document.Id);
            }
            else
            {
                _logger.LogWarning("AI extraction failed for document {DocumentId}: {Errors}",
                    document.Id, string.Join(", ", extractionResult.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId} for AI extraction", document.Id);
            // Don't throw - this is a background process and shouldn't fail the main workflow
        }
    }

    /// <summary>
    /// Prepares document data for AI API processing
    /// </summary>
    private async Task<DocumentExtractionRequest> PrepareDocumentForAIAsync(Document document, IFormFile documentFile)
    {
        // Convert file to base64 for API transmission
        using var memoryStream = new MemoryStream();
        await documentFile.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var base64Content = Convert.ToBase64String(fileBytes);

        // Read configuration for searchable PDF generation
        var generateSearchablePDF = _configuration.GetValue<bool>("AIExtraction:GenerateSearchablePDF", true);

        return new DocumentExtractionRequest
        {
            DocumentId = document.Id,
            FileName = document.Name,
            DocumentType = document.Type ?? "unknown",
            FileContent = base64Content,
            ContentType = documentFile.ContentType,
            WorkItemId = document.OrderId, // Use OrderId from document
            ProcessingOptions = new ExtractionOptions
            {
                ExtractText = true,
                ExtractTables = true,
                ExtractKeyValuePairs = true,
                EnableOCR = true,
                GenerateSearchablePDF = generateSearchablePDF, // Use configuration setting
                Language = "en" // Default to English, could be configurable
            }
        };
    }

    /// <summary>
    /// Sends document to AI API for text extraction
    /// </summary>
    private async Task<DocumentExtractionResult> SendDocumentToAIAPIAsync(DocumentExtractionRequest request)
    {
        try
        {
            // Get AI API configuration
            var aiApiUrl = _configuration["AIExtraction:ApiUrl"];
            var apiKey = _configuration["AIExtraction:ApiKey"];
            var timeout = _configuration.GetValue<int>("AIExtraction:TimeoutSeconds", 300); // 5 minutes default

            if (string.IsNullOrEmpty(aiApiUrl))
            {
                return DocumentExtractionResult.Error("AI API URL not configured");
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            // Add API key to headers if provided
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                // Or use Authorization header if needed
                // httpProject.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Xtract-DocumentProcessor/1.0");

            // Serialize request
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending document {DocumentId} to AI API at {ApiUrl}",
                request.DocumentId, aiApiUrl);

            // Send request to AI API
            var response = await httpClient.PostAsync($"{aiApiUrl}/extract", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DocumentExtractionResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return result ?? DocumentExtractionResult.Error("Invalid response from AI API");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI API returned error {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);

                return DocumentExtractionResult.Error($"AI API error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling AI API for document {DocumentId}", request.DocumentId);
            return DocumentExtractionResult.Error($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling AI API for document {DocumentId}", request.DocumentId);
            return DocumentExtractionResult.Error("AI API request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling AI API for document {DocumentId}", request.DocumentId);
            return DocumentExtractionResult.Error($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates document with extracted data from AI API
    /// </summary>
    private async Task UpdateDocumentWithExtractedDataAsync(Document document, DocumentExtractionResult extractionResult)
    {
        try
        {
            // Update document with extracted text for searchability
            document.SearchableText = extractionResult.ExtractedText;
            document.Pages = extractionResult.PageCount > 0 ? extractionResult.PageCount : document.Pages;

            // Update searchable document URLs if provided by AI API
            if (!string.IsNullOrEmpty(extractionResult.SearchableUrl))
            {
                document.SearchableUrl = extractionResult.SearchableUrl;
            }

            if (!string.IsNullOrEmpty(extractionResult.SearchableBlobName))
            {
                document.SearchableBlobName = extractionResult.SearchableBlobName;
            }

            // Create additional order data from extracted key-value pairs
            if (extractionResult.ExtractedFields?.Any() == true)
            {
                await CreateOrderDataFromExtractedFieldsAsync(document.OrderId, extractionResult.ExtractedFields);
            }

            // Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated document {DocumentId} with extracted text ({TextLength} characters), {FieldCount} extracted fields, SearchableUrl: {SearchableUrl}, SearchableBlobName: {SearchableBlobName}",
                document.Id, document.SearchableText?.Length ?? 0, extractionResult.ExtractedFields?.Count ?? 0,
                document.SearchableUrl ?? "None", document.SearchableBlobName ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} with extracted data", document.Id);
            throw;
        }
    }

    /// <summary>
    /// Creates OrderData records from AI-extracted fields
    /// </summary>
    private async Task CreateOrderDataFromExtractedFieldsAsync(int orderId, List<ExtractedField> extractedFields)
    {
        try
        {
            // Get the order to find project ID for field mapping
            var order = await _context.Orders
                .FirstOrDefaultAsync(w => w.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for extracted field processing", orderId);
                return;
            }

            // Get field mappings for this project
            var fieldMappings = await _context.FieldMappings
                .Where(fm => fm.ProjectId == order.ProjectId)
                .ToListAsync();

            var fieldMappingLookup = new Dictionary<string, int>();
            foreach (var mapping in fieldMappings)
            {
                fieldMappingLookup[mapping.InputField.ToLowerInvariant()] = mapping.SchemaFieldId;
            }

            // Process extracted fields
            foreach (var extractedField in extractedFields)
            {
                var fieldKey = extractedField.Key.ToLowerInvariant();

                // Check if this extracted field maps to a schema field
                if (fieldMappingLookup.TryGetValue(fieldKey, out var schemaFieldId))
                {
                    // Check if order data already exists for this field
                    var existingOrderData = await _context.OrderData
                        .FirstOrDefaultAsync(od => od.OrderId == orderId && od.SchemaFieldId == schemaFieldId);

                    if (existingOrderData == null)
                    {
                        // Create new order data from AI extraction
                        var orderData = new OrderData
                        {
                            OrderId = orderId,
                            SchemaFieldId = schemaFieldId,
                            ProcessedValue = extractedField.Value,
                            ConfidenceScore = (decimal)extractedField.Confidence,
                            CreatedAt = DateTime.UtcNow,
                            // Note: This is AI-extracted, not verified by user
                            IsVerified = false
                        };

                        _context.OrderData.Add(orderData);
                    }
                    else if (string.IsNullOrEmpty(existingOrderData.ProcessedValue))
                    {
                        // Update existing record if it doesn't have processed value
                        existingOrderData.ProcessedValue = extractedField.Value;
                        existingOrderData.ConfidenceScore = (decimal)extractedField.Confidence;
                        existingOrderData.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    _logger.LogDebug("Extracted field '{FieldKey}' has no mapping to schema field for project {ProjectId}",
                        extractedField.Key, order.ProjectId);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created/updated {FieldCount} order data records from AI extraction for order {OrderId}",
                extractedFields.Count, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order data from extracted fields for order {OrderId}", orderId);
            throw;
        }
    }
}