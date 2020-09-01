using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Types;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Communication.Server.Application
{
    public interface IArribaManagementService
    {
        SecureDatabase GetDatabaseForOwner(ITelemetry telemetry, IPrincipal user);

        IEnumerable<string> GetTables();

        IDictionary<string, TableInformation> GetTablesForUser(ITelemetry telemetry, IPrincipal user);

        bool UnloadTableForUser(string tableName, ITelemetry telemetry, IPrincipal user);

        bool UnloadAllTableForUser(ITelemetry telemetry, IPrincipal user);

        TableInformation GetTableInformationForUser(string tableName, ITelemetry telemetry, IPrincipal user);

        TableInformation CreateTableForUser(CreateTableRequest table, ITelemetry telemetry, IPrincipal user);

        void AddColumnsToTableForUser(string tableName, IList<ColumnDetails> columnDetails, ITelemetry telemetry, IPrincipal user);

        (bool, ExecutionDetails) SaveTableForUser(string tableName, ITelemetry telemetry, IPrincipal user, VerificationLevel verificationLevel);

        void ReloadTableForUser(string tableName, ITelemetry telemetry, IPrincipal user);

        void DeleteTableForUser(string tableName, ITelemetry telemetry, IPrincipal user);

        DeleteResult DeleteTableRowsForUser(string tableName, string query, ITelemetry telemetry, IPrincipal user);

        void GrantAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, ITelemetry telemetry, IPrincipal user);

        void RevokeAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, ITelemetry telemetry, IPrincipal user);

    }
}
