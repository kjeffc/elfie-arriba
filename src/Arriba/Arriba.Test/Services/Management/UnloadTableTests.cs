using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow(TableName)]
        public void UnloadTableByUser(string tableName)
        {
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _telemetry, _nonAuthenticatedUser));
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _telemetry, _reader));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _telemetry, _owner));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _telemetry, _writer));
        }
    }
}
