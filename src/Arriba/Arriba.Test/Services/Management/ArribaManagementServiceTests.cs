using Arriba.Model.Column;
using Arriba.Model.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    [TestClass]
    public partial class ArribaManagementServiceTests : ArribaServiceBase
    {
        public ArribaManagementServiceTests() : base()
        {
        }

        private void CheckTableColumnsQuantity(string tableName, int expected)
        {
            var table = _db[tableName];

            Assert.AreEqual(expected, table.ColumnDetails.Count);
        }

        private void AddColumnsToTableForUser(string tableName, IPrincipal user)
        {
            CheckTableColumnsQuantity(tableName, 2);
            var columnList = GetColumnDetailsList();
            _service.AddColumnsToTableForUser(tableName, columnList, _telemetry, user);
            CheckTableColumnsQuantity(tableName, 3);
        }

        private static List<ColumnDetails> GetColumnDetailsList()
        {
            var columnList = new List<ColumnDetails>();
            columnList.Add(new ColumnDetails("Column", "string", ""));
            return columnList;
        }

        private int GetPermissionScopeQuantity(string tableName, PermissionScope permissionScope)
        {
            var security = _db.Security(tableName);
            switch (permissionScope)
            {
                case PermissionScope.Reader: return security.Readers.Count;
                case PermissionScope.Writer: return security.Writers.Count;
                case PermissionScope.Owner: return security.Owners.Count;
            }
            throw new ArribaException("Permission Scope not handled!");
        }

    }
}
