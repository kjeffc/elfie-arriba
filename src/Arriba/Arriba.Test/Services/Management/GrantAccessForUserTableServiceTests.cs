using Arriba.Model.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {
        [DataTestMethod]
        [DataRow("foo")]
        public void GrantAccessForUserTableNotFound(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<TableNotFoundException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void GrantAccessForUserTableNameMissing(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, " ")]
        public void GrantAccessForUserSecurityIdendityMissing(string tableName, IdentityScope scope, string identityName)
        {
            var identity = new SecurityIdentity(scope, identityName);
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void GrantAccessForUserUnauthorizedUser(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _reader));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _telemetry, _writer));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, "group1", PermissionScope.Reader)]
        [DataRow(TableName, IdentityScope.User, "user5", PermissionScope.Writer)]
        [DataRow(TableName, IdentityScope.Group, "group4", PermissionScope.Owner)]
        public void GrantAccessForUserOwner(string tableName, IdentityScope scope, string identityName, PermissionScope permissionScope)
        {
            var countBefore = GetPermissionScopeQuantity(tableName, permissionScope);
            var identity = new SecurityIdentity(scope, identityName);
            _service.GrantAccessForUser(tableName, identity, permissionScope, _telemetry, _owner);
            var countAfter = GetPermissionScopeQuantity(tableName, permissionScope);
            Assert.IsTrue(countBefore < countAfter);
        }
    }
}
