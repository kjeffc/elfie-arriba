using Arriba.Model.Security;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Arriba.Communication.Server.Authorization
{
    public interface IArribaAuthorization
    {
        bool ValidateDatabaseAccessForUser(IPrincipal user, PermissionScope scope);

        bool ValidateTableAccessForUser(string tableName, IPrincipal user, PermissionScope scope);

        bool HasTableAccess(string tableName, IPrincipal currentUser, PermissionScope scope);

        bool HasPermission(SecurityPermissions security, IPrincipal currentUser, PermissionScope scope);

        bool IsInIdentity(IPrincipal currentUser, SecurityIdentity targetUserOrGroup);

        bool ValidateCreateAccessForUser(IPrincipal user);
    }
}
