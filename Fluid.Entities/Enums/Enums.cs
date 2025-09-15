namespace Fluid.Entities.Enums;

public enum UserRole
{
    Admin,
    Manager,
    Operator
}

public enum ClientStatus
{
    Active,
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

public enum OrderStatus
{
    Created,
    ValidationError,
    ReadyForAI,
    AIProcessing,
    ReadyForAssignment,
    Assigned,
    InProgress,
    QCRequired,
    Completed,
    Error
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