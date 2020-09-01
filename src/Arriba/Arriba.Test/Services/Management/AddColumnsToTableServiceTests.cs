using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow("foo")]
        public void AddColumnsToTableForUserTableDoesntExist(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<TableNotFoundException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow(null)]
        public void AddColumnsToTableForUserTableNameMissing(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<ArgumentException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _telemetry, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserNotAuthorized(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _telemetry, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserOwner(string tableName)
        {
            AddColumnsToTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserWriter(string tableName)
        {
            AddColumnsToTableForUser(tableName, _writer);
        }
    }
}
