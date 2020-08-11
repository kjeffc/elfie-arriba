// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Communication;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Types;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Server.Application
{
    internal class ArribaManagementService
    {
        private SecureDatabase Database { get; }
        private ArribaAuthority Authority { get; }

        public ArribaManagementService(SecureDatabase database, ArribaAuthority auth)
        {
            this.Database = database;
            this.Authority = auth;
        }

        public IEnumerable<string> GetTables()
        {
            return this.Database.TableNames;
        }

        public IDictionary<string, TableInformation> GetAllBasics(IPrincipal user)
        {
            IDictionary<string, TableInformation> allBasics = new Dictionary<string, TableInformation>();
            foreach (string tableName in this.Database.TableNames)
            {
                if (Authority.HasTableAccess(tableName, user, PermissionScope.Reader))
                {
                    allBasics[tableName] = GetTableBasics(tableName, user);
                }
            }

            return allBasics;
        }

        public TableInformation GetTableBasics(string tableName, IPrincipal user)
        {
            if (!Authority.HasTableAccess(tableName, user, PermissionScope.Reader))
                return null;

            var table = this.Database[tableName];

            TableInformation ti = new TableInformation();
            ti.Name = tableName;
            ti.PartitionCount = table.PartitionCount;
            ti.RowCount = table.Count;
            ti.LastWriteTimeUtc = table.LastWriteTimeUtc;
            ti.CanWrite = Authority.HasTableAccess(tableName, user, PermissionScope.Writer);
            ti.CanAdminister = Authority.HasTableAccess(tableName, user, PermissionScope.Owner);

            IList<string> restrictedColumns = this.Database.GetRestrictedColumns(tableName, (si) => Authority.IsInIdentity(user, si));
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

        public void UnloadTable(string tableName)
        {
            this.Database.UnloadTable(tableName);
        }

        public void UnloadAll()
        {
            this.Database.UnloadAll();
        }

        public void Drop(ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                this.Database.DropTable(tableName);
            }
        }

        public SecurityPermissions GetTablePermissions(string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            return this.Database.Security(tableName);
        }


        public uint DeleteRows(IExpression query, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            Table table = this.Database[tableName];
            DeleteResult result = table.Delete(query);
            return result.Count;
        }

        public void SetTablePermissions(string tableName, SecurityPermissions security)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            // Reset table permissions and save them
            this.Database.SetSecurity(tableName, security);
            this.Database.SaveSecurity(tableName);
        }

        public void CreateNew(CreateTableRequest createTable, IPrincipal user, ITelemetry telemetry)
        {
            if (createTable == null)
                throw new ArgumentNullException(nameof(createTable));

            if (string.IsNullOrWhiteSpace(createTable.TableName))
                throw new ArgumentException("Invalid table name");

            // Does the table already exist? 
            if (this.Database.TableExists(createTable.TableName))
            {
                throw new TableAlreadyExistsException($"Table {createTable.TableName} already exists");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Create", type: "Table", identity: createTable.TableName, detail: createTable))
            {
                var table = this.Database.AddTable(createTable.TableName, createTable.ItemCountLimit);

                // Add columns from request
                table.AddColumns(createTable.Columns);

                // Include permissions from request
                if (createTable.Permissions != null)
                {
                    // Ensure the creating user is always an owner
                    createTable.Permissions.Grant(IdentityScope.User, user.Identity.Name, PermissionScope.Owner);

                    this.Database.SetSecurity(createTable.TableName, createTable.Permissions);
                }

                // Save, so that table existence, column definitions, and permissions are saved
                table.Save();
                this.Database.SaveSecurity(createTable.TableName);
            }
        }

        public void AddColumns(List<ColumnDetails> columns, ITelemetry telemetry, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Not Provided", nameof(tableName));
            }

            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentException("Not Provided", nameof(columns));
            }
            using (telemetry.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                if (!Database.TableExists(tableName))
                {
                    throw new TableNotFoundException($"Table {tableName} not found");
                }

                Table table = this.Database[tableName];

                table.AddColumns(columns);
            }
        }

        public void Reload(ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                this.Database.ReloadTable(tableName);
            }
        }

        public ExecutionDetails Save(ITelemetry telemetry, string tableName)
        {
            ExecutionDetails d = new ExecutionDetails();

            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName))
            {
                Table t = this.Database[tableName];

                // Verify before saving; don't save if inconsistent
                t.VerifyConsistency(VerificationLevel.Normal, d);

                if (d.Succeeded)
                {
                    t.Save();
                }
            }

            return d;
        }

        public void Revoke(SecurityIdentity identity, PermissionScope scope, ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "RevokePermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Revoke(identity, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }
        }

        public void Grant(SecurityIdentity identity, PermissionScope scope, ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "GrantPermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Grant(identity.Scope, identity.Name, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }
        }
    }
}
