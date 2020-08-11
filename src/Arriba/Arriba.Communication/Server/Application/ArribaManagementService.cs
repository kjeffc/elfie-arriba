// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public IResponse GetTables(IRequestContext ctx, Route route)
        {
            return ArribaResponse.Ok(this.Database.TableNames);
        }

        public IResponse GetAllBasics(IRequestContext ctx)
        {
            bool hasTables = false;

            Dictionary<string, TableInformation> allBasics = new Dictionary<string, TableInformation>();
            foreach (string tableName in this.Database.TableNames)
            {
                hasTables = true;

                if (Authority.HasTableAccess(tableName, ctx.Request.User, PermissionScope.Reader))
                {
                    allBasics[tableName] = GetTableBasics(tableName, ctx);
                }
            }

            // If you didn't have access to any tables, return a distinct result to show Access Denied in the browser
            // but not a 401, because that is eaten by CORS.
            if (allBasics.Count == 0 && hasTables)
            {
                return ArribaResponse.Ok(null);
            }

            return ArribaResponse.Ok(allBasics);
        }

        public IResponse GetTableInformation(IRequestContext ctx, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            TableInformation ti = GetTableBasics(tableName, ctx);
            return ArribaResponse.Ok(ti);
        }

        public TableInformation GetTableBasics(string tableName, IRequestContext ctx)
        {
            if (!Authority.HasTableAccess(tableName, ctx.Request.User, PermissionScope.Reader))
                return null;

            var table = this.Database[tableName];

            TableInformation ti = new TableInformation();
            ti.Name = tableName;
            ti.PartitionCount = table.PartitionCount;
            ti.RowCount = table.Count;
            ti.LastWriteTimeUtc = table.LastWriteTimeUtc;
            ti.CanWrite = Authority.HasTableAccess(tableName, ctx.Request.User, PermissionScope.Writer);
            ti.CanAdminister = Authority.HasTableAccess(tableName, ctx.Request.User, PermissionScope.Owner);

            IList<string> restrictedColumns = this.Database.GetRestrictedColumns(tableName, (si) => Authority.IsInIdentity(ctx.Request.User, si));
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

        public IResponse UnloadTable(IRequestContext ctx, string tableName)
        {
            this.Database.UnloadTable(tableName);
            return ArribaResponse.Ok($"Table unloaded");
        }

        public IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            this.Database.UnloadAll();
            return ArribaResponse.Ok("All Tables unloaded");
        }

        public IResponse Drop(ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                this.Database.DropTable(tableName);
                return ArribaResponse.Ok("Table deleted");
            }
        }

        public IResponse GetTablePermissions(IRequestContext request, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to return security for.");
            }

            var security = this.Database.Security(tableName);
            return ArribaResponse.Ok(security);
        }


        public IResponse DeleteRows(IExpression query, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            Table table = this.Database[tableName];
            DeleteResult result = table.Delete(query);

            return ArribaResponse.Ok(result.Count);
        }

        public async Task<IResponse> SetTablePermissions(IRequestContext request, string tableName)
        {
            SecurityPermissions security = await request.Request.ReadBodyAsync<SecurityPermissions>();

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table doesn't exist to update security for.");
            }

            // Reset table permissions and save them
            this.Database.SetSecurity(tableName, security);
            this.Database.SaveSecurity(tableName);

            return ArribaResponse.Ok("Security Updated");
        }

        public async Task<IResponse> CreateNew(IRequestContext request, ITelemetry telemetry, Route routeData)
        {
            CreateTableRequest createTable = await request.Request.ReadBodyAsync<CreateTableRequest>();

            if (createTable == null)
            {
                return ArribaResponse.BadRequest("Invalid body");
            }

            // Does the table already exist? 
            if (this.Database.TableExists(createTable.TableName))
            {
                return ArribaResponse.BadRequest("Table already exists");
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
                    createTable.Permissions.Grant(IdentityScope.User, request.Request.User.Identity.Name, PermissionScope.Owner);

                    this.Database.SetSecurity(createTable.TableName, createTable.Permissions);
                }

                // Save, so that table existence, column definitions, and permissions are saved
                table.Save();
                this.Database.SaveSecurity(createTable.TableName);
            }

            return ArribaResponse.Ok(null);
        }

        public async Task<IResponse> AddColumns(IRequestContext request, ITelemetry telemetry, string tableName)
        {
            using (telemetry.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                if (!Database.TableExists(tableName))
                {
                    return ArribaResponse.NotFound("Table not found to Add Columns to.");
                }

                Table table = this.Database[tableName];

                List<ColumnDetails> columns = await request.Request.ReadBodyAsync<List<ColumnDetails>>();
                table.AddColumns(columns);

                return ArribaResponse.Ok("Added");
            }
        }

        public IResponse Reload(ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to reload");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                this.Database.ReloadTable(tableName);
                return ArribaResponse.Ok("Reloaded");
            }
        }

        public IResponse Save(ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to save");
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName))
            {
                Table t = this.Database[tableName];

                // Verify before saving; don't save if inconsistent
                ExecutionDetails d = new ExecutionDetails();
                t.VerifyConsistency(VerificationLevel.Normal, d);

                if (d.Succeeded)
                {
                    t.Save();
                    return ArribaResponse.Ok("Saved");
                }
                else
                {
                    return ArribaResponse.Error("Table state inconsistent. Not saving. Restart server to reload. Errors: " + d.Errors);
                }
            }
        }

        public async Task<IResponse> Revoke(IRequestContext request, Route route, ITelemetry telemetry, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to revoke permission on.");
            }

            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();
            if (String.IsNullOrEmpty(identity.Name))
            {
                return ArribaResponse.BadRequest("Identity name must not be empty");
            }

            PermissionScope scope;
            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (telemetry.Monitor(MonitorEventLevel.Information, "RevokePermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Revoke(identity, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok("Revoked");
        }

        public async Task<IResponse> Grant(IRequestContext request, Route route, string tableName)
        {
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to grant permission on.");
            }

            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();
            if (String.IsNullOrEmpty(identity.Name))
            {
                return ArribaResponse.BadRequest("Identity name must not be empty");
            }

            PermissionScope scope;
            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (request.Monitor(MonitorEventLevel.Information, "GrantPermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Grant(identity.Scope, identity.Name, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok("Granted");
        }
    }
}
