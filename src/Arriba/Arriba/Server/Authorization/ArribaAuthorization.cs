using Arriba.Caching;
using Arriba.Model;
using Arriba.Model.Security;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;
using System;
using System.Security.Principal;

namespace Arriba.Communication.Server.Authorization
{
    public class ArribaAuthorization : IArribaAuthorization
    {
        private readonly SecureDatabase _database;
        private readonly ClaimsAuthenticationService _claimsAuth;

        public ArribaAuthorization(SecureDatabase database, ClaimsAuthenticationService claims)
        {
            _database = database;
            _claimsAuth = claims;
        }

        public bool HasTableAccess(string tableName, IPrincipal currentUser, PermissionScope scope)
        {
            var security = _database.Security(tableName);

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
            if (currentUser == null || currentUser.Identity == null || !currentUser.Identity.IsAuthenticated)
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

        public bool ValidateDatabaseAccessForUser(IPrincipal user, PermissionScope scope)
        {
            return HasTableAccess(string.Empty, user, scope);
        }

        public bool ValidateTableAccessForUser(string tableName, IPrincipal user, PermissionScope scope)
        {
            ParamChecker.ThrowIfTableNotFound(_database, tableName);

            return HasTableAccess(tableName, user, scope);
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

        public bool ValidateCreateAccessForUser(IPrincipal user)
        {
            bool hasPermission = false;
            var security = _database.DatabasePermissions();
            if (!security.HasTableAccessSecurity)
            {
                // TODO: CoreBug
                hasPermission = false;
                //// If there's no security, table create is only allowed if the service is running as the same user
                //hasPermission = ctx.Request.User.Identity.Name.Equals(WindowsIdentity.GetCurrent().Name);
            }
            else
            {
                // Otherwise, check for writer or better permissions at the DB level
                hasPermission = HasPermission(security, user, PermissionScope.Writer);
            }

            return hasPermission;

        }
    }
}
