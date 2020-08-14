// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Communication.Server.Authorization;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;

namespace Arriba.Server
{
    public abstract class ArribaApplication : RoutedApplication<IResponse>
    {
        protected static readonly ArribaResponse ContinueToNextHandlerResponse = null;
        private ClaimsAuthenticationService _claimsAuth;
        private ComposedCorrector _correctors;
        private IArribaAuthorization _arribaAuthorization;

        protected ArribaApplication(DatabaseFactory factory, ClaimsAuthenticationService claimsAuth)
        {
            ParamChecker.ThrowIfNull(factory, nameof(factory));
            ParamChecker.ThrowIfNull(claimsAuth, nameof(claimsAuth));

            this.EventSource = EventPublisher.CreateEventSource(this.GetType().Name);
            this.Database = factory.GetDatabase();
            _claimsAuth = claimsAuth;

            _arribaAuthorization = new ArribaAuthorization(this.Database);

            // Cache correctors which aren't request specific
            // Cache the People table so that it isn't reloaded for every request.
            // TODO: Need to make configurable by table.
            _correctors = new ComposedCorrector(new TodayCorrector(), new UserAliasCorrector(this.Database["People"]));
        }

        public override string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }

        protected SecureDatabase Database { get; private set; }

        public EventPublisherSource EventSource { get; private set; }

        /// <summary>
        ///  Return the current Corrector(s) to use to correct queries
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        protected ICorrector CurrentCorrectors(IPrincipal user)
        {
            user.ThrowIfNull(nameof(user));

            if (user.Identity == null)
                throw new ArgumentException("User has no identity", nameof(user));

            // Add the 'MeCorrector' for the requesting user (must be first, to chain with the UserAliasCorrector)
            return new ComposedCorrector(new MeCorrector(user.Identity.Name), _correctors);
        }

        protected Task<IResponse> ValidateBodyAsync(IRequestContext ctx, Route route)
        {
            if (!ctx.Request.HasBody)
            {
                return Task.FromResult<IResponse>(ArribaResponse.BadRequest("Request must have content body"));
            }

            return Task.FromResult<IResponse>(null);
        }

        protected Task<IResponse> ValidateReadAccessAsync(IRequestContext ctx, Route route)
        {
            return Task.FromResult<IResponse>(this.ValidateReadAccess(ctx, route));
        }

        protected Task<IResponse> ValidateWriteAccessAsync(IRequestContext ctx, Route route)
        {
            return Task.FromResult<IResponse>(this.ValidateWriteAccess(ctx, route));
        }

        protected Task<IResponse> ValidateOwnerAccessAsync(IRequestContext ctx, Route route)
        {
            return Task.FromResult<IResponse>(this.ValidateOwnerAccess(ctx, route));
        }

        protected Task<IResponse> ValidateCreateAccessAsync(IRequestContext ctx, Route route)
        {
            return Task.FromResult<IResponse>(this.ValidateCreateAccess(ctx, route));
        }

        protected IResponse ValidateCreateAccess(IRequestContext ctx, Route route)
        {
            var user = ctx.Request.User;

            if (!_arribaAuthorization.ValidateCreateAccessForUser(user))
            {
                return ArribaResponse.Forbidden(String.Format("Create Table access denied for {0}.", user.Identity.Name));
            }

            return ContinueToNextHandlerResponse;

        }

        protected bool ValidateDatabaseAccessForUser(IPrincipal user, PermissionScope scope)
        {
            return _arribaAuthorization.ValidateDatabaseAccessForUser(user, scope);
        }

        protected bool ValidateTableAccessForUser(string tableName, IPrincipal user, PermissionScope scope)
        {
            return _arribaAuthorization.ValidateTableAccessForUser(tableName, user, scope);
        }

        protected IResponse ValidateReadAccess(IRequestContext ctx, Route routeData)
        {
            return this.ValidateTableAccess(ctx, routeData, PermissionScope.Reader);
        }

        protected IResponse ValidateWriteAccess(IRequestContext ctx, Route routeData)
        {
            return this.ValidateTableAccess(ctx, routeData, PermissionScope.Writer);
        }

        protected IResponse ValidateOwnerAccess(IRequestContext ctx, Route routeData)
        {
            return this.ValidateTableAccess(ctx, routeData, PermissionScope.Owner);
        }

        protected Task<IResponse> ValidateTableAccessAsync(IRequestContext ctx, Route routeData, PermissionScope scope, bool overrideLocalHostSameUser = false)
        {
            return Task.FromResult<IResponse>(this.ValidateTableAccess(ctx, routeData, scope, overrideLocalHostSameUser));
        }

        protected IResponse ValidateTableAccess(IRequestContext ctx, Route routeData, PermissionScope scope, bool overrideLocalHostSameUser = false)
        {
            string tableName = GetAndValidateTableName(routeData);
            Database.ThrowIfTableNotFound(tableName);

            var currentUser = ctx.Request.User;

            // If we are asked if override auth, check if the request was made from a loopback address (local) and the 
            // current process identity matches the request identity
            if (overrideLocalHostSameUser && IsRequestOriginLoopback(ctx.Request) && IsProcessUserSame(currentUser.Identity))
            {
                // Log for auditing that we skipped out on checking table auth. 
                this.EventSource.Raise(MonitorEventLevel.Warning,
                                        MonitorEventOpCode.Mark,
                                        entityType: "Table",
                                        entityIdentity: tableName,
                                        name: "Authentication Override",
                                        user: ctx.Request.User.Identity.Name,
                                        detail: "Skipping table authentication for local loopback user on request");

                return ContinueToNextHandlerResponse;
            }

            if (!HasTableAccess(tableName, currentUser, scope))
            {
                return ArribaResponse.Forbidden(String.Format("Access to {0} denied for {1}.", tableName, currentUser.Identity.Name));
            }
            else
            {
                return ContinueToNextHandlerResponse;
            }
        }

        protected bool HasTableAccess(string tableName, IPrincipal currentUser, PermissionScope scope)
        {
            return _arribaAuthorization.HasTableAccess(tableName, currentUser, scope);
        }


        protected bool IsInIdentity(IPrincipal currentUser, SecurityIdentity targetUserOrGroup)
        {
            return _arribaAuthorization.IsInIdentity(currentUser, targetUserOrGroup);
        }

        /// <summary>
        /// Validates the current process identity matches the given identity.
        /// </summary>
        /// <param name="identity">Identity to validate.</param>
        /// <returns>true if same user otherwise false.</returns>
        private static bool IsProcessUserSame(IIdentity identity)
        {
            var processUserName = (String.IsNullOrEmpty(Environment.UserDomainName) ? String.Empty : (Environment.UserDomainName + @"\")) + Environment.UserName;
            return String.Equals(identity.Name, processUserName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates that the given request was made from a loopback address (localhost). 
        /// </summary>
        /// <param name="request">Request to validate.</param>
        /// <returns>true if request was made from a loopback address, otherwise false. </returns>
        private static bool IsRequestOriginLoopback(IRequest request)
        {
            IPAddress ipAddress;
            return IPAddress.TryParse(request.Origin, out ipAddress) && IPAddress.IsLoopback(ipAddress);
        }

        protected string GetAndValidateTableName(Route route)
        {
            string tableName = route["tableName"];

            tableName.ThrowIfNullOrWhiteSpaced(nameof(tableName));

            return tableName;
        }

        protected override IResponse OnAfterProcess(IRequestContext request, IResponse response)
        {
            var arribaResponse = response as ArribaResponse;

            if (arribaResponse != null)
            {
                // Add the request trace timings to the response trace timings so they can be output to the client.
                arribaResponse.ResponseBody.TraceTimings = request.TraceTimings;
            }

            // Always no cache for arriba responses 
            response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate"); // HTTP 1.1
            response.AddHeader("Pragma", "no-cache"); // HTTP 1.0
            response.AddHeader("Expires", "0"); // Proxies

            // Allow cross-domain use [server:80 to server:42784]
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Credentials", "true");

            return response;
        }
    }
}
