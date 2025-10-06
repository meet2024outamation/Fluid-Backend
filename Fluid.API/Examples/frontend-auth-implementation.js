// Frontend Implementation Example for Tenant-Scoped Authentication

class AuthService {
  constructor() {
    this.token = null;
    this.currentUser = null;
    this.selectedTenant = null;
  }

  // Step 1: Authenticate user and get token
  async authenticate(credentials) {
    // Your existing Azure AD authentication logic
    this.token = await getAzureAdToken(credentials);
    return this.token;
  }

  // Step 2: Get accessible tenants for user
  async getAccessibleTenants(userEmail) {
    const response = await fetch(`/api/users/accessible-tenants?userIdentifier=${userEmail}`, {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('Failed to get accessible tenants');
    }

    return await response.json();
  }

  // Step 3: Select tenant and store context
  async selectTenant(tenantIdentifier) {
    this.selectedTenant = tenantIdentifier;
    localStorage.setItem('selectedTenantId', tenantIdentifier);
    
    // Get user profile for selected tenant
    this.currentUser = await this.getCurrentUserProfile();
    
    return this.currentUser;
  }

  // Step 4: Get current user profile with tenant-scoped roles and permissions
  async getCurrentUserProfile() {
    if (!this.selectedTenant) {
      throw new Error('No tenant selected');
    }

    const response = await fetch('/api/users/me', {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'X-Tenant-Id': this.selectedTenant,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      if (response.status === 400) {
        const error = await response.text();
        throw new Error(error || 'Invalid tenant ID');
      }
      throw new Error('Failed to get user profile');
    }

    const userProfile = await response.json();
    this.currentUser = userProfile;
    
    return userProfile;
  }

  // Permission checking utilities
  hasPermission(permissionName) {
    if (!this.currentUser?.permissions) return false;
    return this.currentUser.permissions.some(p => p.name === permissionName);
  }

  hasRole(roleName) {
    if (!this.currentUser?.roles) return false;
    return this.currentUser.roles.some(r => r.roleName === roleName);
  }

  hasAnyRole(roleNames) {
    return roleNames.some(roleName => this.hasRole(roleName));
  }

  hasAnyPermission(permissionNames) {
    return permissionNames.some(permission => this.hasPermission(permission));
  }

  // Get all permissions for current user
  getUserPermissions() {
    return this.currentUser?.permissions?.map(p => p.name) || [];
  }

  // Get all roles for current user
  getUserRoles() {
    return this.currentUser?.roles?.map(r => r.roleName) || [];
  }

  // Check if user is Product Owner (global admin)
  isProductOwner() {
    return this.hasRole('ProductOwner');
  }

  // Check if user is Tenant Admin for current tenant
  isTenantAdmin() {
    return this.hasRole('TenantAdmin');
  }

  // Get current tenant context
  getCurrentTenant() {
    return {
      tenantId: this.currentUser?.currentTenantId,
      tenantName: this.currentUser?.currentTenantName
    };
  }

  // Switch to different tenant
  async switchTenant(newTenantId) {
    await this.selectTenant(newTenantId);
    // Emit event for UI to refresh
    window.dispatchEvent(new CustomEvent('tenantChanged', { 
      detail: { 
        tenantId: newTenantId, 
        userProfile: this.currentUser 
      } 
    }));
  }

  // Logout
  logout() {
    this.token = null;
    this.currentUser = null;
    this.selectedTenant = null;
    localStorage.removeItem('selectedTenantId');
    localStorage.removeItem('authToken');
  }
}

// Usage Examples
class AppComponent {
  constructor() {
    this.authService = new AuthService();
  }

  async handleLogin(credentials) {
    try {
      // Step 1: Authenticate
      await this.authService.authenticate(credentials);
      
      // Step 2: Get accessible tenants
      const tenantsData = await this.authService.getAccessibleTenants(credentials.email);
      
      // Step 3: Show tenant selection if user has multiple tenants
      if (tenantsData.tenants.length > 1) {
        const selectedTenant = await this.showTenantSelectionDialog(tenantsData);
        await this.authService.selectTenant(selectedTenant.tenantIdentifier);
      } else if (tenantsData.tenants.length === 1) {
        // Auto-select single tenant
        await this.authService.selectTenant(tenantsData.tenants[0].tenantIdentifier);
      } else {
        throw new Error('No accessible tenants found');
      }

      // Step 4: Navigate to main application
      this.navigateToMainApp();
      
    } catch (error) {
      this.showError(error.message);
    }
  }

  // UI Permission Helpers
  setupUIBasedOnPermissions() {
    const user = this.authService.currentUser;
    
    // Show/hide menu items based on permissions
    if (this.authService.hasPermission('ManageUsers')) {
      document.getElementById('userManagementMenu').style.display = 'block';
    }
    
    if (this.authService.hasPermission('ManageOrders')) {
      document.getElementById('orderManagementMenu').style.display = 'block';
    }
    
    if (this.authService.hasRole('ProductOwner')) {
      document.getElementById('systemAdminMenu').style.display = 'block';
    }
    
    // Update user info display
    document.getElementById('userName').textContent = user.name;
    document.getElementById('currentTenant').textContent = user.currentTenantName;
    
    // Show role badges
    const roleContainer = document.getElementById('userRoles');
    roleContainer.innerHTML = '';
    user.roles.forEach(role => {
      const badge = document.createElement('span');
      badge.className = 'role-badge';
      badge.textContent = role.roleName;
      roleContainer.appendChild(badge);
    });
  }

  // API Helper with automatic tenant header
  async apiCall(url, options = {}) {
    const defaultOptions = {
      headers: {
        'Authorization': `Bearer ${this.authService.token}`,
        'X-Tenant-Id': this.authService.selectedTenant,
        'Content-Type': 'application/json',
        ...options.headers
      }
    };

    return fetch(url, { ...options, headers: defaultOptions.headers });
  }

  // Permission-based route guard
  canAccessRoute(routeName) {
    const routePermissions = {
      'user-management': ['ManageUsers'],
      'order-management': ['ManageOrders', 'ViewOrders'],
      'system-admin': ['SystemAdmin'],
      'reports': ['ViewReports']
    };

    const requiredPermissions = routePermissions[routeName];
    if (!requiredPermissions) return true;

    return this.authService.hasAnyPermission(requiredPermissions);
  }
}

// Initialize app
const app = new AppComponent();

// Listen for tenant changes
window.addEventListener('tenantChanged', (event) => {
  app.setupUIBasedOnPermissions();
  // Refresh any tenant-specific data
  app.refreshTenantData();
});

// Example: Protecting API calls with permission checks
async function createOrder(orderData) {
  if (!app.authService.hasPermission('ManageOrders')) {
    throw new Error('Insufficient permissions to create orders');
  }

  return app.apiCall('/api/orders', {
    method: 'POST',
    body: JSON.stringify(orderData)
  });
}

// Example: Role-based UI rendering
function renderDashboard() {
  const user = app.authService.currentUser;
  
  if (app.authService.isProductOwner()) {
    return renderProductOwnerDashboard();
  } else if (app.authService.isTenantAdmin()) {
    return renderTenantAdminDashboard();
  } else if (app.authService.hasRole('Keying')) {
    return renderKeyingDashboard();
  } else if (app.authService.hasRole('QC')) {
    return renderQCDashboard();
  } else {
    return renderBasicDashboard();
  }
}