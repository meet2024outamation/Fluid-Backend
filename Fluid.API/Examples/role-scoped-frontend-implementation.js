// Frontend Implementation for Role-Scoped Me API

class RoleScopedAuthService {
  constructor() {
    this.token = null;
    this.currentUser = null;
    this.selectedTenant = null;
    this.selectedProject = null;
  }

  // Authenticate user and get initial profile
  async authenticate(credentials) {
    this.token = await getAzureAdToken(credentials);
    
    // Get initial user info to determine role type
    const accessibleTenants = await this.getAccessibleTenants(credentials.email);
    
    // Determine user's primary role type
    const userRoleType = this.determineUserRoleType(accessibleTenants);
    
    return { userRoleType, accessibleTenants };
  }

  // Determine user's primary role type from accessible tenants response
  determineUserRoleType(accessibleTenantsResponse) {
    if (accessibleTenantsResponse.isProductOwner) {
      return 'ProductOwner';
    } else if (accessibleTenantsResponse.tenantAdminIds?.length > 0) {
      return 'TenantAdmin';
    } else if (accessibleTenantsResponse.tenants?.length > 0) {
      return 'ProjectScoped';
    } else {
      throw new Error('User has no assigned roles');
    }
  }

  // Get current user profile based on role type and selected context
  async getCurrentUserProfile() {
    try {
      const userRoleType = await this.getUserRoleType();
      
      switch (userRoleType) {
        case 'ProductOwner':
          return await this.getProductOwnerProfile();
        case 'TenantAdmin':
          return await this.getTenantAdminProfile();
        case 'ProjectScoped':
          return await this.getProjectScopedProfile();
        default:
          throw new Error(`Unknown user role type: ${userRoleType}`);
      }
    } catch (error) {
      console.error('Error getting user profile:', error);
      throw error;
    }
  }

  // ProductOwner: No context required
  async getProductOwnerProfile() {
    const response = await fetch('/api/users/me', {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('Failed to get ProductOwner profile');
    }

    const profile = await response.json();
    this.currentUser = profile;
    
    console.log('ProductOwner profile loaded:', {
      contextType: profile.contextType,
      roleCount: profile.roles.length,
      permissionCount: profile.permissions.length
    });
    
    return profile;
  }

  // TenantAdmin: Requires tenant context
  async getTenantAdminProfile() {
    if (!this.selectedTenant) {
      throw new Error('TenantAdmin users must select a tenant first');
    }

    const response = await fetch('/api/users/me', {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'X-Tenant-Id': this.selectedTenant,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to get TenantAdmin profile');
    }

    const profile = await response.json();
    this.currentUser = profile;
    
    console.log('TenantAdmin profile loaded:', {
      contextType: profile.contextType,
      tenantId: profile.currentTenantId,
      tenantName: profile.currentTenantName,
      roleCount: profile.roles.length,
      permissionCount: profile.permissions.length
    });
    
    return profile;
  }

  // Project-scoped roles: Requires both tenant and project context
  async getProjectScopedProfile() {
    if (!this.selectedTenant) {
      throw new Error('Project-scoped users must select a tenant first');
    }
    
    if (!this.selectedProject) {
      throw new Error('Project-scoped users must select a project first');
    }

    const response = await fetch(`/api/users/me?projectId=${this.selectedProject}`, {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'X-Tenant-Id': this.selectedTenant,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to get project-scoped profile');
    }

    const profile = await response.json();
    this.currentUser = profile;
    
    console.log('Project-scoped profile loaded:', {
      contextType: profile.contextType,
      tenantId: profile.currentTenantId,
      tenantName: profile.currentTenantName,
      projectId: profile.currentProjectId,
      projectName: profile.currentProjectName,
      roleCount: profile.roles.length,
      permissionCount: profile.permissions.length
    });
    
    return profile;
  }

  // Context selection methods
  async selectTenant(tenantId) {
    this.selectedTenant = tenantId;
    localStorage.setItem('selectedTenantId', tenantId);
    
    // For TenantAdmin, reload profile after tenant selection
    const userRoleType = await this.getUserRoleType();
    if (userRoleType === 'TenantAdmin') {
      await this.getCurrentUserProfile();
    }
  }

  async selectProject(projectId) {
    this.selectedProject = projectId;
    localStorage.setItem('selectedProjectId', projectId);
    
    // For project-scoped roles, reload profile after project selection
    const userRoleType = await this.getUserRoleType();
    if (userRoleType === 'ProjectScoped') {
      await this.getCurrentUserProfile();
    }
  }

  // Permission checking methods
  hasPermission(permissionName) {
    if (!this.currentUser?.permissions) return false;
    return this.currentUser.permissions.some(p => p.name === permissionName);
  }

  hasRole(roleName) {
    if (!this.currentUser?.roles) return false;
    return this.currentUser.roles.some(r => r.roleName === roleName);
  }

  // Context-aware permission checking
  canAccessGlobalAdmin() {
    return this.currentUser?.contextType === 'Global' && 
           this.hasPermission('SystemAdmin');
  }

  canManageTenant() {
    return (this.currentUser?.contextType === 'Global' || 
            this.currentUser?.contextType === 'Tenant') &&
           this.hasPermission('ManageUsers');
  }

  canManageProject() {
    return this.currentUser?.contextType === 'Project' && 
           this.hasPermission('ManageOrders');
  }

  // Get current context information
  getCurrentContext() {
    return {
      type: this.currentUser?.contextType,
      tenantId: this.currentUser?.currentTenantId,
      tenantName: this.currentUser?.currentTenantName,
      projectId: this.currentUser?.currentProjectId,
      projectName: this.currentUser?.currentProjectName
    };
  }

  // Helper methods
  async getUserRoleType() {
    if (!this.currentUser) {
      const accessibleTenants = await this.getAccessibleTenants();
      return this.determineUserRoleType(accessibleTenants);
    }
    
    return this.currentUser.contextType === 'Global' ? 'ProductOwner' :
           this.currentUser.contextType === 'Tenant' ? 'TenantAdmin' : 'ProjectScoped';
  }

  isProductOwner() {
    return this.currentUser?.contextType === 'Global';
  }

  isTenantAdmin() {
    return this.currentUser?.contextType === 'Tenant';
  }

  isProjectScoped() {
    return this.currentUser?.contextType === 'Project';
  }
}

// UI Component for handling different role types
class RoleBasedApp {
  constructor() {
    this.authService = new RoleScopedAuthService();
  }

  async initialize() {
    try {
      // Load saved context from localStorage
      const savedTenant = localStorage.getItem('selectedTenantId');
      const savedProject = localStorage.getItem('selectedProjectId');
      
      if (savedTenant) this.authService.selectedTenant = savedTenant;
      if (savedProject) this.authService.selectedProject = savedProject;
      
      // Load user profile based on role type
      await this.authService.getCurrentUserProfile();
      
      // Setup UI based on user context
      this.setupRoleBasedUI();
      
    } catch (error) {
      console.error('App initialization failed:', error);
      this.handleAuthError(error);
    }
  }

  setupRoleBasedUI() {
    const user = this.authService.currentUser;
    
    // Clear existing UI
    this.clearUI();
    
    switch (user.contextType) {
      case 'Global':
        this.setupProductOwnerUI(user);
        break;
      case 'Tenant':
        this.setupTenantAdminUI(user);
        break;
      case 'Project':
        this.setupProjectScopedUI(user);
        break;
    }
    
    // Setup common UI elements
    this.setupUserInfo(user);
    this.setupPermissionBasedMenus(user);
  }

  setupProductOwnerUI(user) {
    console.log('Setting up ProductOwner UI');
    
    // Show system administration panels
    this.showElement('system-admin-panel');
    this.showElement('tenant-management-panel');
    this.showElement('global-user-management-panel');
    
    // Hide tenant/project selection
    this.hideElement('tenant-selector');
    this.hideElement('project-selector');
    
    // Update breadcrumb
    this.updateBreadcrumb(['System Administration']);
  }

  setupTenantAdminUI(user) {
    console.log('Setting up TenantAdmin UI');
    
    // Show tenant management panels
    this.showElement('tenant-management-panel');
    this.showElement('tenant-user-management-panel');
    this.showElement('project-management-panel');
    
    // Show tenant selector, hide project selector
    this.showElement('tenant-selector');
    this.hideElement('project-selector');
    
    // Update breadcrumb
    this.updateBreadcrumb([
      'Tenant Administration', 
      user.currentTenantName || user.currentTenantId
    ]);
  }

  setupProjectScopedUI(user) {
    console.log('Setting up Project-scoped UI');
    
    // Show project operation panels
    this.showElement('order-management-panel');
    this.showElement('batch-management-panel');
    this.showElement('workflow-management-panel');
    
    // Show both tenant and project selectors
    this.showElement('tenant-selector');
    this.showElement('project-selector');
    
    // Update breadcrumb
    this.updateBreadcrumb([
      'Project Operations',
      user.currentTenantName || user.currentTenantId,
      user.currentProjectName || `Project ${user.currentProjectId}`
    ]);
  }

  setupPermissionBasedMenus(user) {
    // Show/hide menu items based on permissions
    const permissions = user.permissions.map(p => p.name);
    
    // System administration
    this.toggleElement('system-settings-menu', permissions.includes('SystemAdmin'));
    this.toggleElement('role-management-menu', permissions.includes('ManageRoles'));
    
    // User management
    this.toggleElement('user-management-menu', permissions.includes('ManageUsers'));
    this.toggleElement('assign-roles-menu', permissions.includes('AssignRoles'));
    
    // Project management
    this.toggleElement('project-settings-menu', permissions.includes('ManageProjects'));
    this.toggleElement('schema-management-menu', permissions.includes('ManageSchemas'));
    
    // Operations
    this.toggleElement('order-management-menu', permissions.includes('ManageOrders'));
    this.toggleElement('batch-management-menu', permissions.includes('ManageBatches'));
    this.toggleElement('reports-menu', permissions.includes('ViewReports'));
  }

  // Context switching handlers
  async handleTenantChange(tenantId) {
    try {
      await this.authService.selectTenant(tenantId);
      this.setupRoleBasedUI();
      this.showSuccess('Tenant context updated');
    } catch (error) {
      this.showError(`Failed to switch tenant: ${error.message}`);
    }
  }

  async handleProjectChange(projectId) {
    try {
      await this.authService.selectProject(projectId);
      this.setupRoleBasedUI();
      this.showSuccess('Project context updated');
    } catch (error) {
      this.showError(`Failed to switch project: ${error.message}`);
    }
  }

  // Utility methods
  showElement(id) {
    const element = document.getElementById(id);
    if (element) element.style.display = 'block';
  }

  hideElement(id) {
    const element = document.getElementById(id);
    if (element) element.style.display = 'none';
  }

  toggleElement(id, show) {
    const element = document.getElementById(id);
    if (element) element.style.display = show ? 'block' : 'none';
  }

  updateBreadcrumb(items) {
    const breadcrumb = document.getElementById('breadcrumb');
    if (breadcrumb) {
      breadcrumb.innerHTML = items.join(' > ');
    }
  }

  setupUserInfo(user) {
    const userNameElement = document.getElementById('user-name');
    const contextInfoElement = document.getElementById('context-info');
    
    if (userNameElement) userNameElement.textContent = user.name;
    if (contextInfoElement) {
      const context = this.authService.getCurrentContext();
      contextInfoElement.textContent = this.formatContextInfo(context);
    }
  }

  formatContextInfo(context) {
    switch (context.type) {
      case 'Global':
        return 'System Administrator';
      case 'Tenant':
        return `Tenant: ${context.tenantName}`;
      case 'Project':
        return `${context.tenantName} > ${context.projectName}`;
      default:
        return 'Unknown Context';
    }
  }

  clearUI() {
    // Hide all panels initially
    ['system-admin-panel', 'tenant-management-panel', 'project-management-panel',
     'order-management-panel', 'batch-management-panel', 'workflow-management-panel'
    ].forEach(id => this.hideElement(id));
  }

  handleAuthError(error) {
    console.error('Authentication error:', error);
    this.showError(error.message);
    // Redirect to login or show appropriate error UI
  }

  showSuccess(message) {
    // Implementation for success notifications
    console.log('Success:', message);
  }

  showError(message) {
    // Implementation for error notifications  
    console.error('Error:', message);
  }
}

// Initialize the application
const app = new RoleBasedApp();

// Start the app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  app.initialize();
});

// Export for use in other modules
export { RoleScopedAuthService, RoleBasedApp };