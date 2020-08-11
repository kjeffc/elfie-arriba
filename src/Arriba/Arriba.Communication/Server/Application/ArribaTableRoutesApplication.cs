// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Types;

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
            return ArribaResponse.Ok(this.Database.TableNames);
        }

        private IResponse GetAllBasics(IRequestContext ctx, Route route)
        {
            return Service.GetAllBasics(ctx);
        }

        private IResponse GetTableInformation(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            return Service.GetTableInformation(ctx, tableName);
        }

        private IResponse UnloadTable(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            return Service.UnloadTable(ctx, tableName);
        }

        private IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            return Service.UnloadAll(ctx, route);
        }

        private IResponse Drop(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            return Service.Drop(ctx, tableName);
        }

        private IResponse GetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return Service.GetTablePermissions(request, tableName);
        }

        private IResponse DeleteRows(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            IExpression query = SelectQuery.ParseWhere(ctx.Request.ResourceParameters["q"]);

            // Run server correctors
            query = this.CurrentCorrectors(ctx).Correct(query);
            return Service.DeleteRows(query, tableName);
        }

        private async Task<IResponse> SetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return await Service.SetTablePermissions(request, tableName);
        }

        private async Task<IResponse> CreateNew(IRequestContext request, Route routeData)
        {
            return await Service.CreateNew(request, request, routeData);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        private async Task<IResponse> AddColumns(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return await Service.AddColumns(request, request, tableName);
        }

        /// <summary>
        /// Reload the specified table.
        /// </summary>
        private IResponse Reload(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return Service.Reload(request, tableName);
        }

        /// <summary>
        /// Saves the specified table.
        /// </summary>
        private IResponse Save(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return Service.Save(request, tableName);
        }

        /// <summary>
        /// Revokes access to a table. 
        /// </summary>
        private async Task<IResponse> Revoke(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return await Service.Revoke(request, route, request, tableName);
        }

        /// <summary>
        /// Grants access to a table. 
        /// </summary>
        private async Task<IResponse> Grant(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            return await Service.Grant(request, route, tableName);
        }
    }
}
