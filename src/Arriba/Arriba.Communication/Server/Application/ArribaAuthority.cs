using Arriba.Model;
using Arriba.Model.Security;
using Arriba.Server.Authentication;
using System;
using System.Security.Principal;

namespace Arriba.Server
{
    internal class ArribaAuthority
    {
        private SecureDatabase Database { get; }
        private readonly ClaimsAuthenticationService _claimsAuth;

        public ArribaAuthority(SecureDatabase database, ClaimsAuthenticationService claimsAuth)
        {
            this.Database = database;
            this._claimsAuth = claimsAuth;
        }

        public bool HasTableAccess(string tableName, IPrincipal currentUser, PermissionScope scope)
        {
            var security = this.Database.Security(tableName);

            // No security? Allowed.
            if (!security.HasTableAccessSecurity)
            {
                return true;
            }

            // Otherwise check permissions
            return HasPermission(security, currentUser, scope);
        }

        public bool HasPermission(SecurityPermissions security, IPrincipal currentUser, PermissionScope scope)
        {
            // No user identity? Forbidden! 
            if (currentUser == null || !currentUser.Identity.IsAuthenticated)
            {
                return false;
            }

            // Try user first, cheap check. 
            if (security.IsIdentityInPermissionScope(IdentityScope.User, currentUser.Identity.Name, scope))
            {
                return true;
            }

            // See if the user is in any allowed groups.
            foreach (var group in security.GetScopeIdentities(scope, IdentityScope.Group))
            {
                if (_claimsAuth.IsUserInGroup(currentUser, group.Name))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInIdentity(IPrincipal currentUser, SecurityIdentity targetUserOrGroup)
        {
            if (targetUserOrGroup.Scope == IdentityScope.User)
            {
                return targetUserOrGroup.Name.Equals(currentUser.Identity.Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return _claimsAuth.IsUserInGroup(currentUser, targetUserOrGroup.Name);
            }
        }
    }
}
