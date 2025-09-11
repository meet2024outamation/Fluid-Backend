using System.Security.Claims;

namespace SharedKernel.Utility
{
    public class Utility
    {
        public class TenantStatusTypes
        {
            public static string Inprogress = "Inprogress";
            public static string Cancelled = "Cancelled";
            public static string Active = "Active";
            public static string Inactive = "Inactive";
        }

        public class TenantTypes
        {
            public static string Lawfirm = "Law firm";
            public static string Servicer = "Servicer";
            public static string Vendor = "Vendor";
            public static string Inactive = "Investor";
        }

        public class UserTypes
        {
            public static string User = "User";
            public static string ServicePrincipal = "ServicePrincipal";

        }

        /// <summary>
        /// Get nested property value using dot separated path
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyPath"></param>
        /// <returns></returns>
        public static object? GetNestedPropertyValue(object obj, string propertyPath)
        {
            var properties = propertyPath.Split('.');
            object? nestedValue = obj;

            foreach (var property in properties)
            {
                var propertyInfo = nestedValue?.GetType().GetProperty(property);
                nestedValue = propertyInfo?.GetValue(nestedValue, null);
            }

            return nestedValue;
        }
        public static bool IsServicePrincipalUser(IEnumerable<Claim> claims)
        {
            if (claims is null)
                throw new ArgumentNullException(nameof(claims));

            // Check for 'roles' claim (indicates Service Principal token)
            var rolesClaim = claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase));
            if (rolesClaim != null)
            {
                return true; // Service Principal token
            }

            // Secondary Check: Absence of 'scp' and presence of specific conditions
            var scpClaim = claims.FirstOrDefault(c => c.Type.Equals("scp", StringComparison.OrdinalIgnoreCase));
            if (scpClaim == null)
            {
                var azpClaim = claims.FirstOrDefault(c => c.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value;
                var subClaim = claims.FirstOrDefault(c => c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase))?.Value;

                if (!string.IsNullOrEmpty(azpClaim) && !string.IsNullOrEmpty(subClaim))
                {
                    return true; // Service Principal token
                }
            }

            // Otherwise, it's an Interactive User token
            return false;
        }
    }

}
