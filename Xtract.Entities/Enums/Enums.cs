namespace Xtract.Entities.Enums;

public enum UserRole
{
    Admin,
    Manager,
    Operator
}

public enum ClientStatus
{
    Setup,
    Mapping,
    Ready,
    Inactive
}

public enum BatchStatus
{
    Received,
    Processing,
    Ready,
    InProgress,
    Completed,
    Error
}

public enum WorkItemStatus
{
    Created,
    ValidationError,
    ReadyForAI,
    AIProcessing,
    AICompleted,
    ReadyForAssignment,
    Assigned,
    InProgress,
    QCRequired,
    Completed,
    Error
}

public enum SchemaFieldDataType
{
    String,
    Integer,
    Decimal,
    Date,
    Boolean
}

public enum AuditAction
{
    INSERT,
    UPDATE,
    DELETE
}

public enum DataSource
{
    InputFile,
    AIExtraction,
    ManualEntry
}