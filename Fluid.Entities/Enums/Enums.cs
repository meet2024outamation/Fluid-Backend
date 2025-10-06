namespace Fluid.Entities.Enums;

public enum UserRole
{
    Admin,
    Keying,
    QC
}

public enum ProjectStatus
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
    KeyingInProgress,
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