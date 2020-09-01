using System;
using System.Collections.Generic;
using System.Security.Principal;
using Arriba.Server.Authorization;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Correctors;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;
using Arriba.Types;
using Arriba.Monitoring;

namespace Arriba.Communication.Server.Application
{
    public class ArribaManagementService : IArribaManagementService
    {
        private readonly SecureDatabase _database;
        private readonly IArribaAuthorization _arribaAuthorization;
        private readonly ICorrector _correctors;

        public ArribaManagementService(SecureDatabase secureDatabase, ICorrector composedCorrector, ClaimsAuthenticationService claims, ISecurityConfiguration securityConfiguration)
        {
            _database = secureDatabase;
            _arribaAuthorization = new ArribaAuthorizationGrantDecorator(_database, claims, securityConfiguration);
            _correctors = composedCorrector;
        }

        private ICorrector CurrentCorrectors(IPrincipal user)
        {
            user.ThrowIfNull(nameof(user));

            if (user.Identity == null)
                throw new ArgumentException("User has no identity", nameof(user));

            // Add the 'MeCorrector' for the requesting user (must be first, to chain with the UserAliasCorrector)
            return new ComposedCorrector(new MeCorrector(user.Identity.Name), _correctors);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnDetails">ColumnDetails List</param>
        /// <param name="user">User requesting the operation</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TableNotFoundException"></exception>
        /// <exception cref="ArribaAccessForbiddenException"></exception>
        public void AddColumnsToTableForUser(string tableName, IList<ColumnDetails> columnDetails, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
                ParamChecker.ThrowIfNull(columnDetails, nameof(columnDetails));

                if (columnDetails.Count == 0)
                    throw new ArgumentException("Not Provided", nameof(columnDetails));

                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                    throw new ArribaAccessForbiddenException("User not authorized");

                Table table = _database[tableName];
                table.AddColumns(columnDetails);
            }

        }

        public TableInformation CreateTableForUser(CreateTableRequest createTable, ITelemetry telemetry, IPrincipal user)
        {

            ParamChecker.ThrowIfNull(createTable, nameof(createTable));
            createTable.TableName.ThrowIfNullOrWhiteSpaced(nameof(createTable));

            using (telemetry.Monitor(MonitorEventLevel.Information, "Create", type: "Table", identity: createTable.TableName, detail: createTable))
            {
                if (!_arribaAuthorization.ValidateCreateAccessForUser(user))
                    throw new ArribaAccessForbiddenException($"Create Table access denied.");

                _database.ThrowIfTableAlreadyExists(createTable.TableName);

                var table = _database.AddTable(createTable.TableName, createTable.ItemCountLimit);

                // Add columns from request
                table.AddColumns(createTable.Columns);

                // Include permissions from request
                if (createTable.Permissions != null)
                {
                    // Ensure the creating user is always an owner
                    createTable.Permissions.Grant(IdentityScope.User, user.Identity.Name, PermissionScope.Owner);

                    _database.SetSecurity(createTable.TableName, createTable.Permissions);
                }

                // Save, so that table existence, column definitions, and permissions are saved
                table.Save();
                _database.SaveSecurity(createTable.TableName);

                return GetTableInformationForUser(createTable.TableName, telemetry, user);
            }
        }

        public void DeleteTableForUser(string tableName, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                    throw new ArribaAccessForbiddenException("Operation not authorized");

                _database.DropTable(tableName);
            }
        }

        public DeleteResult DeleteTableRowsForUser(string tableName, string query, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "DeleteTableRowsForUser", type: "Table", identity: tableName))
            {
                tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
                query.ThrowIfNullOrWhiteSpaced(nameof(query));
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                    throw new ArribaAccessForbiddenException("User not authorized");

                var table = _database[tableName];
                var expression = SelectQuery.ParseWhere(query);
                var correctExpression = this.CurrentCorrectors(user).Correct(expression);
                return table.Delete(correctExpression);
            }
        }

        public SecureDatabase GetDatabaseForOwner(ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "GetDatabase", type: "Database"))
            {
                if (!_arribaAuthorization.ValidateDatabaseAccessForUser(user, PermissionScope.Owner))
                    throw new ArribaAccessForbiddenException("User has no be an owner to retrieve the database");

                return _database;
            }
        }

        public TableInformation GetTableInformationForUser(string tableName, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "GetTableInformationForUser", type: "Table", identity: tableName))
            {
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                    return null;

                var table = this._database[tableName];

                if (table == null)
                    return null;

                TableInformation ti = new TableInformation();
                ti.Name = tableName;
                ti.PartitionCount = table.PartitionCount;
                ti.RowCount = table.Count;
                ti.LastWriteTimeUtc = table.LastWriteTimeUtc;
                ti.CanWrite = _arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer);
                ti.CanAdminister = _arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Owner);

                IList<string> restrictedColumns = _database.GetRestrictedColumns(tableName, (si) => _arribaAuthorization.IsInIdentity(user, si));
                if (restrictedColumns == null)
                {
                    ti.Columns = table.ColumnDetails;
                }
                else
                {
                    List<ColumnDetails> allowedColumns = new List<ColumnDetails>();
                    foreach (ColumnDetails column in table.ColumnDetails)
                    {
                        if (!restrictedColumns.Contains(column.Name)) allowedColumns.Add(column);
                    }
                    ti.Columns = allowedColumns;
                }

                return ti;
            }
        }

        public IEnumerable<string> GetTables()
        {
            return this._database.TableNames;
        }

        public IDictionary<string, TableInformation> GetTablesForUser(ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "GetTablesForUser", type: "Table"))
            {
                IDictionary<string, TableInformation> allBasics = new Dictionary<string, TableInformation>();
                foreach (string tableName in _database.TableNames)
                {
                    if (_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                    {
                        allBasics[tableName] = GetTableInformationForUser(tableName, telemetry, user);
                    }
                }

                return allBasics;
            }
        }

        public void GrantAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "GrantPermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = securityIdentity }))
            {
                CheckAuthorizationPreCondition(tableName, securityIdentity, user);

                SecurityPermissions security = _database.Security(tableName);
                security.Grant(securityIdentity.Scope, securityIdentity.Name, scope);

                // Save permissions
                _database.SaveSecurity(tableName);
            }
        }

        private void CheckAuthorizationPreCondition(string tableName, SecurityIdentity securityIdentity, IPrincipal user)
        {
            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
            _database.ThrowIfTableNotFound(tableName);
            ParamChecker.ThrowIfNull(securityIdentity, nameof(securityIdentity));
            securityIdentity.Name.ThrowIfNullOrWhiteSpaced(nameof(securityIdentity.Name));

            if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Owner))
                throw new ArribaAccessForbiddenException("Operation not authorized");
        }

        public void ReloadTableForUser(string tableName, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                    throw new ArribaAccessForbiddenException("Operation not authorized");

                _database.ReloadTable(tableName);
            }
        }

        public void RevokeAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "RevokeAccess", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = securityIdentity }))
            {
                CheckAuthorizationPreCondition(tableName, securityIdentity, user);

                SecurityPermissions security = _database.Security(tableName);
                security.Revoke(securityIdentity.Scope, securityIdentity.Name, scope);

                _database.SaveSecurity(tableName);
            }
        }

        public (bool, ExecutionDetails) SaveTableForUser(string tableName, ITelemetry telemetry, IPrincipal user, VerificationLevel verificationLevel)
        {
            bool tableSaved = false;
            using (telemetry.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName, detail: verificationLevel))
            {
                tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                    throw new ArribaAccessForbiddenException("Not authorized");

                Table table = _database[tableName];

                ExecutionDetails executionDetails = new ExecutionDetails();
                table.VerifyConsistency(verificationLevel, executionDetails);

                if (executionDetails.Succeeded)
                {
                    table.Save();
                    tableSaved = true;
                }

                return (tableSaved, executionDetails);
            }
        }

        public bool UnloadAllTableForUser(ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "UnloadAllTables", type: "Table"))
            {
                if (!_arribaAuthorization.ValidateCreateAccessForUser(user))
                    return false;

                _database.UnloadAll();

                return true;
            }
        }

        public bool UnloadTableForUser(string tableName, ITelemetry telemetry, IPrincipal user)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "UnloadTable", type: "Table", identity: tableName))
            {
                _database.ThrowIfTableNotFound(tableName);

                if (!_arribaAuthorization.ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                    return false;

                _database.UnloadTable(tableName);

                return true;
            }
        }
    }
}
