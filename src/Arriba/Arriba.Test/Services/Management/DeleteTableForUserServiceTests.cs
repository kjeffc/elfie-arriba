using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow("foo")]
        public void DeleteTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.DeleteTableForUser(tableName, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void DeleteTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.DeleteTableForUser(tableName, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.DeleteTableForUser(tableName, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserOwner(string tableName)
        {
            DeleteTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserWriter(string tableName)
        {
            DeleteTableForUser(tableName, _owner);
        }
    }
}
