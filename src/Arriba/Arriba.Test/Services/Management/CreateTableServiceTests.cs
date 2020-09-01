using Arriba.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [TestMethod]
        public void CreateTableForUserNullObject()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _telemetry, _nonAuthenticatedUser));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _telemetry, _reader));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _telemetry, _writer));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void CreateTableForUserWithInvalidName(string tableName)
        {
            var tableRequest = new CreateTableRequest(tableName, 1000);

            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _nonAuthenticatedUser));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _reader));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _writer));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserNotAuthorized(string tableName)
        {
            var tableRequest = new CreateTableRequest($"{tableName}_notauthorized", 1000);

            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.CreateTableForUser(tableRequest, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserOwner(string tableName)
        {
            CreateTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserWriter(string tableName)
        {
            CreateTableForUser(tableName, _writer);
        }

        private void CreateTableForUser(string table, IPrincipal user)
        {
            var tableName = $"{table}_{user.Identity.Name}";

            DeleteTable(_db, tableName);

            var tableOwner = _service.CreateTableForUser(new CreateTableRequest(tableName, 1000), _telemetry, user);
            Assert.IsNotNull(tableOwner);
            Assert.IsTrue(tableOwner.CanAdminister);
            Assert.IsTrue(tableOwner.CanWrite);

            DeleteTable(_db, tableName);

        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserTableAlreadyExists(string tableName)
        {
            Assert.ThrowsException<TableAlreadyExistsException>(() => _service.CreateTableForUser(new CreateTableRequest(tableName, 1000), _telemetry, _owner));
        }
    }
}
