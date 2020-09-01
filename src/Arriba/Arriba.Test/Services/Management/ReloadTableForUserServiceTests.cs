using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow("foo")]
        public void ReloadTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.ReloadTableForUser(tableName, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void ReloadTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.ReloadTableForUser(tableName, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void ReloadTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.ReloadTableForUser(tableName, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void ReloadTableForUser(string tableName)
        {
            _service.ReloadTableForUser(tableName, _telemetry, _reader);
            _service.ReloadTableForUser(tableName, _telemetry, _writer);
            _service.ReloadTableForUser(tableName, _telemetry, _owner);
        }
    }
}
