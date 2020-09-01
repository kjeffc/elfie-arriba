using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {
        [DataTestMethod]
        [DataRow(TableName)]
        public void GetTableInformationForUser(string tableName)
        {
            var table = _service.GetTableInformationForUser(tableName, _telemetry, _nonAuthenticatedUser);
            Assert.IsNull(table);

            var tableOwner = _service.GetTableInformationForUser(tableName, _telemetry, _owner);
            Assert.IsNotNull(tableOwner);
            Assert.IsTrue(tableOwner.CanAdminister);
            Assert.IsTrue(tableOwner.CanWrite);

            var tableWriter = _service.GetTableInformationForUser(tableName, _telemetry, _writer);
            Assert.IsNotNull(tableWriter);
            Assert.IsFalse(tableWriter.CanAdminister);
            Assert.IsTrue(tableWriter.CanWrite);

            var tableReader = _service.GetTableInformationForUser(tableName, _telemetry, _reader);
            Assert.IsNotNull(tableReader);
            Assert.IsFalse(tableReader.CanAdminister);
            Assert.IsFalse(tableReader.CanWrite);
        }
    }
}
