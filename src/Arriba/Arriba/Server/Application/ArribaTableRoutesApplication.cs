// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Types;

namespace Arriba.Server.Application
{
    public class ArribaTableRoutesApplication : ArribaApplication
    {
        private readonly IArribaManagementService _service;

        public ArribaTableRoutesApplication(DatabaseFactory f, ClaimsAuthenticationService auth, IArribaManagementService managementService)
            : base(f, auth)
        {
            _service = managementService;

            // GET - return tables in Database
            this.Get("", this.GetTables);

            this.Get("/allBasics", this.GetAllBasics);

            this.Get("/unloadAll", this.UnloadAll);

            // GET /table/foo - Get table information 
            this.Get("/table/:tableName", this.GetTableInformation);

            // POST /table with create table payload (Must be Writer/Owner in security directly in DiskCache folder, or identity running service)
            this.PostAsync("/table", this.CreateNew);

            // POST /table/foo/addcolumns
            this.PostAsync("/table/:tableName/addcolumns", this.AddColumns);

            // GET /table/foo/save -- TODO: This is not ideal, think of a better pattern 
            this.Get("/table/:tableName/save", this.Save);

            // Unload/Reload
            this.Get("/table/:tableName/unload", this.UnloadTable);
            this.Get("/table/:tableName/reload", this.Reload);

            // DELETE /table/foo 
            this.Delete("/table/:tableName", this.Drop);
            this.Get("/table/:tableName/delete", this.Drop);

            // POST /table/foo?action=delete
            this.Get(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.DeleteRows);
            this.Post(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.DeleteRows);

            // POST /table/foo/permissions/user - add permissions 
            this.PostAsync("/table/:tableName/permissions/:scope", this.Grant);

            // DELETE /table/foo/permissions/user - remove permissions from table 
            this.DeleteAsync("/table/:tableName/permissions/:scope", this.Revoke);

            // NOTE: _SPECIAL_ permission for localhost users, will override current auth to always be valid.
            // this enables tables recovery from local machine for matching user as the process. 
            // GET /table/foo/permissions  
            this.Get("/table/:tableName/permissions",
                    (c, r) => this.ValidateTableAccess(c, r, PermissionScope.Reader, overrideLocalHostSameUser: true),
                    this.GetTablePermissions);

            // POST /table/foo/permissions  
            this.PostAsync("/table/:tableName/permissions",
                     async (c, r) => await this.ValidateTableAccessAsync(c, r, PermissionScope.Owner, overrideLocalHostSameUser: true),
                     this.SetTablePermissions);
        }

        private IResponse GetTables(IRequestContext ctx, Route route)
        {
            return ArribaResponse.Ok(_service.GetTables());
        }

        private IResponse GetAllBasics(IRequestContext ctx, Route route)
        {
            IPrincipal user = ctx.Request.User;

            IDictionary<string, TableInformation> allBasics = _service.GetTablesForUser(user);

            return ArribaResponse.Ok(allBasics);
        }

        private IResponse GetTableInformation(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var tableInformation = _service.GetTableInformationForUser(tableName, ctx.Request.User);

            if (tableInformation == null)
                return ArribaResponse.NotFound();

            return ArribaResponse.Ok(tableInformation);
        }

        private IResponse UnloadTable(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);

            if (_service.UnloadTableForUser(tableName, ctx.Request.User))
                return ArribaResponse.Ok($"Table {tableName} unloaded");
            else
                return ArribaResponse.Forbidden($"Not able to unload table {tableName}");
        }

        private IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            if (!_service.UnloadAllTableForUser(ctx.Request.User))
                return ArribaResponse.Forbidden("Not able to unload all tables");

            return ArribaResponse.Ok("All Tables unloaded");
        }

        private IResponse Drop(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;

            using (ctx.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                try
                {
                    _service.DeleteTableForUser(tableName, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
                return ArribaResponse.Ok("Table deleted");
            }
        }

        private IResponse GetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to return security for.");
            }

            var security = this.Database.Security(tableName);
            return ArribaResponse.Ok(security);
        }


        private IResponse DeleteRows(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var query = ctx.Request.ResourceParameters["q"];
            var user = ctx.Request.User;

            try
            {
                var result = _service.DeleteTableRowsForUser(tableName, query, user);
                return ArribaResponse.Ok(result.Count);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

        }

        private async Task<IResponse> SetTablePermissions(IRequestContext request, Route route)
        {
            SecurityPermissions security = await request.Request.ReadBodyAsync<SecurityPermissions>();
            string tableName = GetAndValidateTableName(route);

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table doesn't exist to update security for.");
            }

            // Reset table permissions and save them
            this.Database.SetSecurity(tableName, security);
            this.Database.SaveSecurity(tableName);

            return ArribaResponse.Ok("Security Updated");
        }

        private async Task<IResponse> CreateNew(IRequestContext request, Route routeData)
        {
            CreateTableRequest createTable = await request.Request.ReadBodyAsync<CreateTableRequest>();
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "Create", type: "Table", identity: createTable.TableName, detail: createTable))
            {
                try
                {
                    _service.CreateTableForUser(createTable, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
            }

            return ArribaResponse.Created(createTable.TableName);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        private async Task<IResponse> AddColumns(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                List<ColumnDetails> columns = await request.Request.ReadBodyAsync<List<ColumnDetails>>();
                try
                {
                    _service.AddColumnsToTableForUser(tableName, columns, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

                return ArribaResponse.Created("Added");
            }
        }

        /// <summary>
        /// Reload the specified table.
        /// </summary>
        private IResponse Reload(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                try
                {
                    _service.ReloadTableForUser(tableName, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

                return ArribaResponse.Ok("Reloaded");
            }
        }

        /// <summary>
        /// Saves the specified table.
        /// </summary>
        private IResponse Save(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);

            using (request.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName))
            {
                try
                {
                    var saveOperation = _service.SaveTableForUser(tableName, request.Request.User, VerificationLevel.Normal);

                    if (!saveOperation.Item1)
                    {
                        return ArribaResponse.Error("Table state is inconsistent. Not saving. Restart server to reload. Errors: " + saveOperation.Item2.Errors);
                    }
                    return ArribaResponse.Ok("Saved");
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

            }
        }

        private IResponse ExceptionToArribaResponse(Exception ex)
        {
            ParamChecker.ThrowIfNull(ex, nameof(ex));

            if (ex is ArribaAccessForbiddenException)
                return ArribaResponse.Forbidden(ex.Message);

            if (ex is TableNotFoundException)
                return ArribaResponse.NotFound(ex.Message);

            return ArribaResponse.BadRequest(ex.Message);
        }

        private enum AuthorizationOperation
        {
            Grant = 1,
            Revoke = 2
        }

        private async Task<IResponse> ExecuteAuthorizationPermission(AuthorizationOperation operation, IRequestContext request, Route route)
        {
            var user = request.Request.User;
            string tableName = GetAndValidateTableName(route);
            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();

            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out var scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (request.Monitor(MonitorEventLevel.Information, $"{operation}Permission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                try
                {
                    if (operation == AuthorizationOperation.Grant)
                        _service.GrantAccessForUser(tableName, identity, scope, user);
                    else
                        _service.RevokeAccessForUser(tableName, identity, scope, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
                SecurityPermissions security = this.Database.Security(tableName);
                security.Revoke(identity, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok($"{operation} succeeded");
        }

        /// <summary>
        /// Revokes access to a table. 
        /// </summary>
        private async Task<IResponse> Revoke(IRequestContext request, Route route)
        {
            return await ExecuteAuthorizationPermission(AuthorizationOperation.Revoke, request, route);
        }

        /// <summary>
        /// Grants access to a table. 
        /// </summary>
        private async Task<IResponse> Grant(IRequestContext request, Route route)
        {
            return await ExecuteAuthorizationPermission(AuthorizationOperation.Revoke, request, route);
        }

        private static string SanitizeIdentity(string rawIdentity)
        {
            if (String.IsNullOrEmpty(rawIdentity))
            {
                throw new ArgumentException("Identity must not be empty", "rawIdentity");
            }

            return rawIdentity.Replace("/", "\\");
        }
    }
}
