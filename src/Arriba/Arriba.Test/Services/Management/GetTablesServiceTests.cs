using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {
        [TestMethod]
        public void GetTables()
        {
            var tables = _service.GetTables();
            Assert.IsNotNull(tables);
        }

        [TestMethod]
        public void GetTablesByUser()
        {
            var tables = _service.GetTablesForUser(_telemetry, _nonAuthenticatedUser);
            Assert.IsNotNull(tables);
            Assert.AreEqual(0, tables.Count);

            var tablesReader = _service.GetTablesForUser(_telemetry, _reader);
            Assert.IsNotNull(tablesReader);
            Assert.IsTrue(tablesReader.Count <= _service.GetTables().Count());

            var tablesWriter = _service.GetTablesForUser(_telemetry, _writer);
            Assert.IsNotNull(tablesWriter);

            var tablesOwner = _service.GetTablesForUser(_telemetry, _owner);
            Assert.IsNotNull(tablesOwner);
            Assert.AreEqual(tablesOwner.Count, _service.GetTables().Count());

        }

        [TestMethod]
        public void GetTablesByNonAuthenticatedUser()
        {
            var tables = _service.GetTablesForUser(_telemetry, _nonAuthenticatedUser);
            Assert.IsNotNull(tables);
            Assert.AreEqual(0, tables.Count);
        }
    }
}
