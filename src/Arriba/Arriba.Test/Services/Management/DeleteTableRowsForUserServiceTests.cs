using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow("foo")]
        public void DeleteTableRowsForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void DeleteTableRowsForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableRowsForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName, "ID = 1 OR Name = Vinicius")]
        [DataRow(TableName, "ID = 2")]
        [DataRow(TableName, "ID = 99")]
        public void DeleteTableRowsForUserOwner(string tableName, string query)
        {
            DeleteTableRowsForUser(tableName, query, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName, "ID = 1 OR Name = Vinicius")]
        [DataRow(TableName, "ID = 2")]
        [DataRow(TableName, "ID = 99")]
        public void DeleteTableRowsForUserWriter(string tableName, string query)
        {
            DeleteTableRowsForUser(tableName, query, _owner);
        }

        private void DeleteTableRowsForUser(string tableName, string query, IPrincipal user)
        {
            var table = _db[tableName];
            var countBefore = table.Count;
            var result = _service.DeleteTableRowsForUser(tableName, query, _telemetry, user);
            if (result.Count > 0)
                Assert.AreEqual(countBefore - result.Count, table.Count);
            else
                Assert.AreEqual(countBefore, table.Count);
        }
    }
}
