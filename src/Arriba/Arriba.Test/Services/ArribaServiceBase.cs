using Arriba.Communication.Server.Application;
using Arriba.Composition;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Structures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Claims;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    [TestClass]
    public partial class ArribaServiceBase
    {
        //Without specifying a type identity.IsAuthenticated always returns false
        private const string AuthenticationType = "TestAuthenticationType";
        protected const string TableName = "Users";

        protected readonly SecureDatabase _db;

        protected readonly ClaimsPrincipal _nonAuthenticatedUser;
        protected readonly ClaimsPrincipal _owner;
        protected readonly ClaimsPrincipal _reader;
        protected readonly ClaimsPrincipal _writer;

        protected readonly IArribaManagementService _service;
        protected readonly ITelemetry _telemetry;

        protected readonly IServiceProvider _serviceProvider;

        public ArribaServiceBase()
        {
            ArribaServices.Initialize();
            var securityConfiguration = new ArribaServerConfiguration();
            securityConfiguration.EnabledAuthentication = true;

            _telemetry = new Arriba.Monitoring.Telemetry(MonitorEventLevel.Verbose, "TEST", null);

            CreateTestDatabase(TableName);

            _serviceProvider = InitServiceProvider(securityConfiguration);

            _nonAuthenticatedUser = new ClaimsPrincipal();
            _reader = GetAuthenticatedUser("user1", PermissionScope.Reader);
            _writer = GetAuthenticatedUser("user2", PermissionScope.Writer);
            _owner = GetAuthenticatedUser("user3", PermissionScope.Owner);

            _service = _serviceProvider.GetService<IArribaManagementService>();
            _db = _service.GetDatabaseForOwner(_telemetry, _owner);
        }

        private IServiceProvider InitServiceProvider(ISecurityConfiguration securityConfiguration)
        {
            var factory = new DefaultServiceProviderFactory();
            var services = new ServiceCollection();
            services.AddArribaServices(securityConfiguration);
            return factory.CreateServiceProvider(services);
        }

        private void CreateTestDatabase(string tableName)
        {
            SecureDatabase db = new SecureDatabase();

            if (db.TableExists(tableName)) db.DropTable(tableName);

            Table t = db.AddTable(tableName, 100);
            t.AddColumn(new ColumnDetails("ID", "int", null, null, true));
            t.AddColumn(new ColumnDetails("Name", "string", ""));

            DataBlock b = new DataBlock(new string[] { "ID", "Name" }, 4,
                new Array[]
                {
                    new int[] { 1, 2, 3, 4},
                    new string[] { "visouza", "ericmai", "louvau", "scott"}
                });
            t.AddOrUpdate(b);
            t.Save();

            //For database
            SetSecurityGroup(db, string.Empty);
            //For table
            SetSecurityGroup(db, tableName);
        }

        private void SetSecurityGroup(SecureDatabase db, string tableName)
        {
            SecurityPermissions security = db.Security(tableName);

            security.Readers.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Reader.ToString()));
            security.Writers.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Writer.ToString()));
            security.Owners.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Owner.ToString()));
            db.SaveSecurity(tableName);
        }

        protected void DeleteTable(SecureDatabase db, string tableName)
        {
            if (db.TableExists(tableName))
                db.DropTable(tableName);
        }

        [TestCleanup]
        public void DeleteDatabaseTestTables()
        {
            foreach (var table in _db.TableNames)
            {
                DeleteTable(_db, table);
            }
        }

        private ClaimsPrincipal GetAuthenticatedUser(string userName, PermissionScope scope)
        {
            var identity = new ClaimsIdentity(new GenericIdentity(userName, AuthenticationType));
            identity.AddClaim(new Claim(identity.RoleClaimType, scope.ToString().ToLower()));
            var user = new ClaimsPrincipal(identity);

            return user;
        }

        protected void DeleteTableForUser(string tableName, IPrincipal user)
        {
            _service.DeleteTableForUser(tableName, _telemetry, user);
        }

    }
}
