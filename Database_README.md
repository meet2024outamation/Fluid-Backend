# Xtract Database Entities

This project contains the Entity Framework Core entities and database context for the Xtract application.

## Database Schema

The database contains the following main entities:

### Core Entities

1. **User** - Extends Azure AD user information
   - Roles: Admin, Manager, Operator
   - Tracks login history and active status

2. **Client** - Represents document processing clients
   - Status: Setup, Mapping, Ready, Inactive
   - Has unique client code

3. **Schema** - Defines field definitions for document extraction
   - JSON-based field definitions
   - Version control and active status

4. **SchemaField** - Individual field definitions within a schema
   - Data types: String, Integer, Decimal, Date, Boolean
   - Configurable format, display order, and required status
   - Unique field names and display order per schema

5. **ClientSchema** - Associates clients with schemas

6. **Batch** - Groups of documents to be processed
   - Status: Received, Processing, Ready, InProgress, Completed, Error
   - Tracks total and processed orders

7. **WorkItem** - Individual document processing tasks (formerly Orders)
   - Status: Created, Validation Error, Ready for AI, AI Processing, AI Completed, Ready for Assignment, Assigned, In Progress, QC Required, Completed, Error
   - Priority scale 1-10 (default 5)
   - External reference for client's reference number
   - Time tracking: assigned, started, completed, estimated completion
   - JSONB validation errors storage

8. **WorkItemData** - Structured data extracted from work items
   - Links work items to schema fields
   - Stores raw and processed values
   - Data source tracking: Input File, AI Extraction, Manual Entry
   - AI confidence scoring and manual verification
   - Unique constraint per work item and schema field

9. **Document** - Actual document files associated with work items
   - Stores file information, URLs, and searchable text

10. **FieldMapping** - Maps input fields to schema fields
    - Contains transformation rules as JSON

11. **AuditLog** - Tracks all system changes
    - Actions: INSERT, UPDATE, DELETE
    - Stores old and new values as JSONB
    - Records user actions, IP addresses, and user agents
    - Optional user reference (nullable for system actions)

## Database Configuration

The application uses PostgreSQL as the database provider. Configure your connection string in:

- `appsettings.json` for production
- `appsettings.Development.json` for development

Example connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=XtractDb;Username=your_username;Password=your_password"
  }
}
```

## Entity Framework Migrations

To create and apply migrations:

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project Xtract.Entities --startup-project Xtract.API

# Update the database
dotnet ef database update --project Xtract.Entities --startup-project Xtract.API
```

## Key Features

- **Integer Primary Keys**: All entities use auto-incrementing integer primary keys for better performance
- **Type Safety**: Uses enums for status fields, data types, data sources, and audit actions with automatic string conversion
- **Relationships**: Properly configured foreign keys and navigation properties
- **Constraints**: Unique indexes on important fields (AzureAdId, Client Code, Schema Field Names, WorkItem Data)
- **Advanced Auditing**: Comprehensive audit logging with before/after values stored as JSONB
- **Structured Data Management**: WorkItemData provides field-level data tracking with verification capabilities
- **Soft Deletes**: Uses delete behaviors to maintain referential integrity

## Entity Relationships

```
User (1) -> (*) Client (CreatedBy)
User (1) -> (*) Schema (CreatedBy)
User (1) -> (*) Batch (CreatedBy)
User (1) -> (*) WorkItem (AssignedTo) [Optional]
User (1) -> (*) WorkItemData (VerifiedBy) [Optional]
User (1) -> (*) FieldMapping (CreatedBy)
User (1) -> (*) AuditLog (ChangedBy) [Optional]

Client (1) -> (*) ClientSchema
Schema (1) -> (*) ClientSchema
Schema (1) -> (*) SchemaField
Schema (1) -> (*) FieldMapping
SchemaField (1) -> (*) WorkItemData

Client (1) -> (*) Batch
Client (1) -> (*) WorkItem
Client (1) -> (*) FieldMapping

Batch (1) -> (*) WorkItem
WorkItem (1) -> (*) Document
WorkItem (1) -> (*) WorkItemData
```

## WorkItem Status Workflow

The `WorkItem` entity supports a comprehensive status workflow:

1. **Created** - Initial status when work item is created
2. **Validation Error** - Input validation failed
3. **Ready for AI** - Passed validation, ready for AI processing
4. **AI Processing** - Currently being processed by AI
5. **AI Completed** - AI processing finished
6. **Ready for Assignment** - Ready to be assigned to a user
7. **Assigned** - Assigned to a user but not yet started
8. **In Progress** - User is actively working on the item
9. **QC Required** - Quality control review needed
10. **Completed** - Work item fully processed
11. **Error** - Processing error occurred

## WorkItemData Structure

The `WorkItemData` entity provides structured field-level data management:

### Data Sources
- **Input File**: Data extracted from uploaded files
- **AI Extraction**: Data extracted by AI processing
- **Manual Entry**: Data entered manually by users

### Data Processing
- **Raw Value**: Original unprocessed data
- **Processed Value**: Cleaned and formatted data
- **Confidence Score**: AI confidence (0.0000-1.0000) or manual certainty
- **Verification**: Manual verification by users with timestamp

### Constraints
- Unique constraint on `(WorkItemId, SchemaFieldId)` ensures one data entry per field per work item
- Foreign key constraints maintain referential integrity

## SchemaField Data Types

The `SchemaField` entity supports the following data types:

- **String**: Text values with optional format specifications
- **Integer**: Whole number values
- **Decimal**: Decimal/floating-point number values
- **Date**: Date values with configurable formats (mm-dd-yyyy, dd/mm/yyyy, etc.)
- **Boolean**: True/false values

Each field can be marked as required and has a configurable display order for UI rendering.

## Audit Logging

The `AuditLog` entity provides comprehensive change tracking:

### Audit Actions
- **INSERT**: Record creation
- **UPDATE**: Record modification  
- **DELETE**: Record deletion

### Audit Data
- **OldValues**: JSONB containing the previous state of the record (for UPDATE and DELETE)
- **NewValues**: JSONB containing the new state of the record (for INSERT and UPDATE)
- **ChangedBy**: Optional reference to the user who made the change (nullable for system actions)
- **ChangedAt**: Timestamp when the change occurred
- **IpAddress**: IP address of the user making the change
- **UserAgent**: Browser/client information

This allows for complete audit trails and the ability to reconstruct the history of any record in the system.

## Notes

- All entities use integer primary keys with auto-increment (Identity columns)
- Timestamps are stored in UTC
- JSON and JSONB fields are used for flexible data storage (metadata, extracted data, validation results, audit values)
- The database is designed to support multi-tenant scenarios through client isolation
- Comprehensive audit logging captures all changes with before/after states for compliance and debugging
- Schema fields provide structured field definitions as an alternative to JSON-based field definitions
- Work item data provides field-level tracking with verification capabilities for quality assurance