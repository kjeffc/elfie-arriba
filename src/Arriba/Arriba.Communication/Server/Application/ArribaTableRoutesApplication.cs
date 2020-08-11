// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Types;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;

namespace Arriba.Server.Application
{
    [Export(typeof(IRoutedApplication))]
    internal class ArribaTableRoutesApplication : ArribaApplication
    {
        private ArribaManagementService Service { get; }

        [ImportingConstructor]
        public ArribaTableRoutesApplication(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            this.Service = new ArribaManagementService(this.Database, this.Authority);

            // GET - return tables in Database
            this.Get("", this.GetTables);

            this.Get("/allBasics", this.GetAllBasics);

            this.Get("/unloadAll", this.ValidateCreateAccess, this.UnloadAll);

            // GET /table/foo - Get table information 
            this.Get("/table/:tableName", this.ValidateReadAccess, this.GetTableInformation);

            // POST /table with create table payload (Must be Writer/Owner in security directly in DiskCache folder, or identity running service)
            this.PostAsync("/table", this.ValidateCreateAccessAsync, this.ValidateBodyAsync, this.CreateNew);

            // POST /table/foo/addcolumns
            this.PostAsync("/table/:tableName/addcolumns", this.ValidateWriteAccessAsync, this.AddColumns);

            // GET /table/foo/save -- TODO: This is not ideal, think of a better pattern 
            this.Get("/table/:tableName/save", this.ValidateWriteAccess, this.Save);

            // Unload/Reload
            this.Get("/table/:tableName/unload", this.ValidateWriteAccess, this.UnloadTable);
            this.Get("/table/:tableName/reload", this.ValidateWriteAccess, this.Reload);

            // DELETE /table/foo 
            this.Delete("/table/:tableName", this.ValidateOwnerAccess, this.Drop);
            this.Get("/table/:tableName/delete", this.ValidateOwnerAccess, this.Drop);

            // POST /table/foo?action=delete
            this.Get(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);
            this.Post(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);

            // POST /table/foo/permissions/user - add permissions 
            this.PostAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Grant);

            // DELETE /table/foo/permissions/user - remove permissions from table 
            this.DeleteAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Revoke);

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
            return ArribaResponse.Ok(Service.GetTables());
        }

        private IResponse GetAllBasics(IRequestContext ctx, Route route)
        {
            return ArribaResponse.Ok(Service.GetAllBasics(ctx.Request.User));
        }

        private IResponse GetTableInformation(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var response = ArribaResponse.NotFound();
            var tableInformation = Service.GetTableBasics(tableName, ctx.Request.User);

            if (tableInformation != null)
            {
                response = ArribaResponse.Ok(tableInformation);
            }

            return response;
        }
        private IResponse UnloadTable(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            Service.UnloadTable(tableName);
            return ArribaResponse.Ok("Table unloaded");
        }

        private IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            Service.UnloadAll();
            return ArribaResponse.Ok("All Tables unloaded");
        }

        private IResponse Drop(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            Service.Drop(ctx, tableName);
            return ArribaResponse.Ok("Table deleted");
        }

        private IResponse GetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return ArribaResponse.Ok(Service.GetTablePermissions(tableName));
        }

        private IResponse DeleteRows(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            IExpression query = SelectQuery.ParseWhere(ctx.Request.ResourceParameters["q"]);

            // Run server correctors
            query = this.CurrentCorrectors(ctx).Correct(query);
            return ArribaResponse.Ok(Service.DeleteRows(query, tableName));
        }

        private async Task<IResponse> SetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            SecurityPermissions security = await request.Request.ReadBodyAsync<SecurityPermissions>();
            Service.SetTablePermissions(tableName, security);
            return ArribaResponse.Ok("Security Updated");
        }

        private async Task<IResponse> CreateNew(IRequestContext request, Route routeData)
        {
            CreateTableRequest createTable = await request.Request.ReadBodyAsync<CreateTableRequest>();
            Service.CreateNew(createTable, request.Request.User, request);
            return ArribaResponse.Created(createTable.TableName);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        private async Task<IResponse> AddColumns(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var columns = await request.Request.ReadBodyAsync<List<ColumnDetails>>();
            Service.AddColumns(columns, request, tableName);
            return ArribaResponse.Created("Added");
        }

        /// <summary>
        /// Reload the specified table.
        /// </summary>
        private IResponse Reload(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            Service.Reload(request, tableName);
            return ArribaResponse.Ok("Reloaded");
        }

        private IResponse Save(IRequestContext request, Route route)
        {
            IResponse response = ArribaResponse.Ok("Saved");
            string tableName = GetAndValidateTableName(route);
            var details = Service.Save(request, tableName);

            if (!details.Succeeded)
            {
                response = ArribaResponse.Error("Table state is inconsistent. Not saving. Restart server to reload. Errors: " + details.Errors);
            }

            return response;
        }

        /// <summary>
        /// Revokes access to a table. 
        /// </summary>
        private async Task<IResponse> Revoke(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
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
            Service.Revoke(identity, scope, request, tableName);
         
            return ArribaResponse.Ok("Revoked");
        }

        /// <summary>
        /// Grants access to a table. 
        /// </summary>
        private async Task<IResponse> Grant(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
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

            Service.Grant(identity, scope, request, tableName);
            return ArribaResponse.Ok("Granted");
        }
    }
}
