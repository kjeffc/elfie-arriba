using Arriba.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow("foo")]
        public void SaveTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.SaveTableForUser(tableName, _telemetry, _owner, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void SaveTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.SaveTableForUser(tableName, _telemetry, _owner, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.SaveTableForUser(tableName, _telemetry, _nonAuthenticatedUser, VerificationLevel.Normal));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.SaveTableForUser(tableName, _telemetry, _reader, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUserInconsistent(string tableName)
        {
            CheckTableColumnsQuantity(tableName, 2);
            _service.AddColumnsToTableForUser(tableName, GetColumnDetailsList(), _telemetry, _owner);

            var result = _service.SaveTableForUser(tableName, _telemetry, _owner, VerificationLevel.Normal);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Item1);
            Assert.IsFalse(result.Item2.Succeeded);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUser(string tableName)
        {
            var result = _service.SaveTableForUser(tableName, _telemetry, _owner, VerificationLevel.Normal);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Item1);
            Assert.IsTrue(result.Item2.Succeeded);
        }
    }
}
