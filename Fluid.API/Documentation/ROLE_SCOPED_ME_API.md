# Role-Scoped Me API Documentation

## Overview

The Me API has been updated to implement different role and permission scoping rules based on the user's role type. This provides more precise access control and better security by ensuring users only see relevant permissions for their current context.

## Role Scoping Rules

### 1. ProductOwner (Global Role)
- **Context**: Global system access
- **Requirements**: No context parameters needed
- **Behavior**: Returns global roles and permissions only
- **Ignores**: Both tenant and project context parameters

### 2. TenantAdmin (Tenant-Scoped Role)
- **Context**: Tenant-level management
- **Requirements**: Requires `X-Tenant-Id` header
- **Behavior**: Returns tenant-scoped roles and permissions
- **Ignores**: Project context (projectId parameter)

### 3. Project-Scoped Roles (Keying, QC, etc.)
- **Context**: Project-level operations
- **Requirements**: Requires both `X-Tenant-Id` header AND `projectId` query parameter
- **Behavior**: Returns project-specific roles and permissions
- **Validates**: User has access to the specified project

## API Endpoint

```http
GET /api/users/me
Headers:
  Authorization: Bearer {jwt-token}
  X-Tenant-Id: {tenant-identifier}    // Required for TenantAdmin and project-scoped roles
Query Parameters:
  projectId: {project-id}              // Required for project-scoped roles
```

## Response Structure

### Common Response Fields
```json
{
  "id": 1,
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+1234567890",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "name": "John Doe",
  "contextType": "Global|Tenant|Project",
  "roles": [...],
  "permissions": [...],
  // Context-specific fields below
}
```

### ProductOwner (Global Context)
```json
{
  // ... common fields ...
  "contextType": "Global",
  "currentTenantId": null,
  "currentTenantName": null,
  "currentProjectId": null,
  "currentProjectName": null,
  "roles": [
    {
      "roleId": 1,
      "roleName": "ProductOwner",
      "description": "Product Owner with full system control"
    }
  ],
  "permissions": [
    { "id": 1, "name": "SystemAdmin", "description": "Full system administration" },
    { "id": 2, "name": "ManageRoles", "description": "Manage system roles" },
    { "id": 3, "name": "ManagePermissions", "description": "Manage system permissions" },
    { "id": 4, "name": "ManageUsers", "description": "Manage system users" },
    { "id": 5, "name": "ManageTenants", "description": "Manage tenants" }
  ]
}
```

### TenantAdmin (Tenant Context)
```json
{
  // ... common fields ...
  "contextType": "Tenant",
  "currentTenantId": "tenant1-identifier",
  "currentTenantName": "Tenant 1 Name",
  "currentProjectId": null,
  "currentProjectName": null,
  "roles": [
    {
      "roleId": 2,
      "roleName": "TenantAdmin",
      "description": "Administrator with tenant access"
    }
  ],
  "permissions": [
    { "id": 6, "name": "ViewRoles", "description": "View roles in tenant" },
    { "id": 7, "name": "AssignRoles", "description": "Assign roles to users" },
    { "id": 8, "name": "ManageUsers", "description": "Manage tenant users" },
    { "id": 9, "name": "ManageProjects", "description": "Manage tenant projects" },
    { "id": 10, "name": "ManageSchemas", "description": "Manage tenant schemas" }
  ]
}
```

### Project-Scoped Role (Project Context)
```json
{
  // ... common fields ...
  "contextType": "Project",
  "currentTenantId": "tenant1-identifier",
  "currentTenantName": "Tenant 1 Name", 
  "currentProjectId": 1,
  "currentProjectName": "Sample Project",
  "roles": [
    {
      "roleId": 3,
      "roleName": "Keying",
      "description": "Keying role with project and resource management access"
    }
  ],
  "permissions": [
    { "id": 11, "name": "ViewUsers", "description": "View project users" },
    { "id": 12, "name": "ManageOrders", "description": "Manage project orders" },
    { "id": 13, "name": "ManageBatches", "description": "Manage project batches" },
    { "id": 14, "name": "ManageOrderFlows", "description": "Manage order workflows" },
    { "id": 15, "name": "ViewReports", "description": "View project reports" }
  ]
}
```

## Usage Examples

### ProductOwner User
```javascript
// ProductOwner can call Me API without any context
const response = await fetch('/api/users/me', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

// Response will have contextType: "Global" with system-wide permissions
const profile = await response.json();
console.log(profile.contextType); // "Global"
console.log(profile.permissions); // SystemAdmin, ManageRoles, etc.
```

### TenantAdmin User
```javascript
// TenantAdmin must provide tenant context
const response = await fetch('/api/users/me', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Id': selectedTenantId
  }
});

// Response will have contextType: "Tenant" with tenant-scoped permissions
const profile = await response.json();
console.log(profile.contextType); // "Tenant"
console.log(profile.currentTenantId); // "tenant1-identifier"
console.log(profile.permissions); // ManageUsers, ManageProjects, etc.
```

### Keying/QC User
```javascript
// Project-scoped roles must provide both tenant and project context
const response = await fetch(`/api/users/me?projectId=${projectId}`, {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Id': selectedTenantId
  }
});

// Response will have contextType: "Project" with project-scoped permissions
const profile = await response.json();
console.log(profile.contextType); // "Project"
console.log(profile.currentProjectId); // 1
console.log(profile.permissions); // ManageOrders, ViewReports, etc.
```

## Error Scenarios

### Missing Required Context

#### TenantAdmin without tenant header:
```http
GET /api/users/me
Authorization: Bearer {token}

Response: 400 Bad Request
{
  "validationErrors": [
    {
      "key": "TenantId",
      "errorMessage": "TenantAdmin users must provide X-Tenant-Id header."
    }
  ]
}
```

#### Project-scoped role without project parameter:
```http
GET /api/users/me
Authorization: Bearer {token}
X-Tenant-Id: tenant1

Response: 400 Bad Request
{
  "validationErrors": [
    {
      "key": "ProjectId", 
      "errorMessage": "Project-scoped roles require projectId query parameter."
    }
  ]
}
```

### Invalid Context

#### Invalid tenant ID:
```http
GET /api/users/me
Authorization: Bearer {token}
X-Tenant-Id: invalid-tenant

Response: 400 Bad Request
{
  "validationErrors": [
    {
      "key": "TenantId",
      "errorMessage": "Tenant with ID 'invalid-tenant' not found or inactive."
    }
  ]
}
```

#### Invalid project ID:
```http
GET /api/users/me?projectId=999999
Authorization: Bearer {token}
X-Tenant-Id: tenant1

Response: 400 Bad Request
{
  "validationErrors": [
    {
      "key": "ProjectId",
      "errorMessage": "Project with ID '999999' not found or inactive in tenant 'tenant1'."
    }
  ]
}
```

#### No access to project:
```http
GET /api/users/me?projectId=2
Authorization: Bearer {token}  
X-Tenant-Id: tenant1

Response: 400 Bad Request
{
  "validationErrors": [
    {
      "key": "Access",
      "errorMessage": "User has no roles assigned to project '2' in tenant 'tenant1'."
    }
  ]
}
```

## Permission Aggregation Logic

### ProductOwner
1. **Include**: Only global roles (TenantId = null, ProjectId = null)
2. **Aggregate**: All permissions from ProductOwner roles
3. **Result**: System-wide administrative permissions

### TenantAdmin  
1. **Include**: Global roles + tenant-specific roles (TenantId = selected tenant)
2. **Aggregate**: All permissions from included roles
3. **Result**: Global permissions + tenant management permissions

### Project-Scoped Roles
1. **Include**: Global roles + specific project roles (TenantId = selected tenant, ProjectId = selected project)
2. **Aggregate**: All permissions from included roles
3. **Result**: Global permissions + project-specific operational permissions

## Frontend Integration

### Role-Based UI Rendering
```javascript
class UserProfileService {
  async getCurrentUserProfile(tenantId, projectId) {
    const user = await this.getCurrentUser();
    
    // Determine which context to request based on user's roles
    if (user.isProductOwner) {
      // ProductOwner - no context needed
      return this.callMeAPI();
    } else if (user.isTenantAdmin) {
      // TenantAdmin - tenant context only
      return this.callMeAPI(tenantId);
    } else {
      // Project-scoped - both contexts required
      return this.callMeAPI(tenantId, projectId);
    }
  }

  async callMeAPI(tenantId = null, projectId = null) {
    const headers = { 'Authorization': `Bearer ${this.token}` };
    
    if (tenantId) {
      headers['X-Tenant-Id'] = tenantId;
    }
    
    let url = '/api/users/me';
    if (projectId) {
      url += `?projectId=${projectId}`;
    }
    
    const response = await fetch(url, { headers });
    return await response.json();
  }
}
```

### Context-Aware Permission Checking
```javascript
function setupUI(userProfile) {
  // Show different menu items based on context type and permissions
  
  if (userProfile.contextType === 'Global') {
    // ProductOwner UI - show system administration
    showSystemAdminMenu(userProfile.permissions);
  } else if (userProfile.contextType === 'Tenant') {
    // TenantAdmin UI - show tenant management
    showTenantAdminMenu(userProfile.permissions);
  } else if (userProfile.contextType === 'Project') {
    // Project-scoped UI - show project operations
    showProjectMenu(userProfile.permissions);
  }
}

function hasPermission(userProfile, permission) {
  return userProfile.permissions.some(p => p.name === permission);
}
```

## Security Benefits

1. **Principle of Least Privilege**: Users only see permissions relevant to their current context
2. **Context Validation**: System validates user has access to requested tenant/project
3. **Dynamic Scoping**: Permission sets change based on selected context
4. **Clear Boundaries**: Explicit separation between global, tenant, and project permissions
5. **Audit Trail**: Logging tracks which context users are operating in

## Migration Guide

### Breaking Changes
- Me API now requires context parameters for non-ProductOwner roles
- Response structure includes new `contextType` field
- Permission sets may be different based on context

### Frontend Updates Required
1. Update Me API calls to include appropriate context parameters
2. Handle new response structure with `contextType`
3. Implement context-aware permission checking
4. Add error handling for missing context parameters

### Testing Checklist
- [ ] ProductOwner users can access Me API without context
- [ ] TenantAdmin users require tenant context
- [ ] Project-scoped roles require both tenant and project context
- [ ] Invalid context parameters return appropriate errors
- [ ] Permission aggregation works correctly for each role type
- [ ] Context validation prevents unauthorized access