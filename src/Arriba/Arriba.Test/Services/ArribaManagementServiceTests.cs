using Arriba.Caching;
using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    [TestClass]
    public partial class ArribaManagementServiceTests
    {
        //Without specifying a type identity.IsAuthenticated always returns false
        private const string AuthenticationType = "TestAuthenticationType";
        private const string TableName = "Users";

        private readonly SecureDatabase _db;

        private readonly ClaimsPrincipal _nonAuthenticatedUser;
        private readonly ClaimsPrincipal _owner;
        private readonly ClaimsPrincipal _reader;
        private readonly ClaimsPrincipal _writer;

        private readonly IArribaManagementService _service;
        private readonly DatabaseFactory _databaseFactory;
        public ArribaManagementServiceTests()
        {
            CreateTestDatabase(TableName);

            _nonAuthenticatedUser = new ClaimsPrincipal();
            _reader = GetAuthenticatedUser("user1", PermissionScope.Reader);
            _writer = GetAuthenticatedUser("user2", PermissionScope.Writer);
            _owner = GetAuthenticatedUser("user3", PermissionScope.Owner);

            _databaseFactory = new DatabaseFactory();
            var claimsAuth = new ClaimsAuthenticationService(new MemoryCacheFactory());
            var factory = new ArribaManagementServiceFactory(_databaseFactory.GetDatabase(), claimsAuth);

            _service = factory.CreateArribaManagementService("Users");
            _db = _service.GetDatabaseForOwner(_owner);
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

        private void DeleteTable(SecureDatabase db, string tableName)
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

        private void CheckTableColumnsQuantity(string tableName, int expected)
        {
            var table = _db[tableName];

            Assert.AreEqual(expected, table.ColumnDetails.Count);
        }

        private void AddColumnsToTableForUser(string tableName, IPrincipal user)
        {
            CheckTableColumnsQuantity(tableName, 2);
            var columnList = GetColumnDetailsList();
            _service.AddColumnsToTableForUser(tableName, columnList, user);
            CheckTableColumnsQuantity(tableName, 3);
        }

        private static List<ColumnDetails> GetColumnDetailsList()
        {
            var columnList = new List<ColumnDetails>();
            columnList.Add(new ColumnDetails("Column", "string", ""));
            return columnList;
        }

        private void DeleteTableForUser(string tableName, IPrincipal user)
        {
            _service.DeleteTableForUser(tableName, user);
        }

        private int GetPermissionScopeQuantity(string tableName, PermissionScope permissionScope)
        {
            var security = _db.Security(tableName);
            switch (permissionScope)
            {
                case PermissionScope.Reader: return security.Readers.Count;
                case PermissionScope.Writer: return security.Writers.Count;
                case PermissionScope.Owner: return security.Owners.Count;
            }
            throw new ArribaException("Permission Scope not handled!");
        }

    }
}
