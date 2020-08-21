using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Types;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Communication.Server.Application
{
    public interface IArribaManagementService
    {
        SecureDatabase GetDatabaseForOwner(IPrincipal user);

        IEnumerable<string> GetTables();

        IDictionary<string, TableInformation> GetTablesForUser(IPrincipal user);

        bool UnloadTableForUser(string tableName, IPrincipal user);

        bool UnloadAllTableForUser(IPrincipal user);

        TableInformation GetTableInformationForUser(string tableName, IPrincipal user);

        TableInformation CreateTableForUser(CreateTableRequest table, IPrincipal user);

        void AddColumnsToTableForUser(string tableName, IList<ColumnDetails> columnDetails, IPrincipal user);

        (bool, ExecutionDetails) SaveTableForUser(string tableName, IPrincipal user, VerificationLevel verificationLevel);

        void ReloadTableForUser(string tableName, IPrincipal user);

        void DeleteTableForUser(string tableName, IPrincipal user);

        DeleteResult DeleteTableRowsForUser(string tableName, string query, IPrincipal user);

        void GrantAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user);

        void RevokeAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user);

    }
}
