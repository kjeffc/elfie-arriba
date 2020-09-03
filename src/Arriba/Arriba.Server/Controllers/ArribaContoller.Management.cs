using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Types;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Arriba.Controllers
{
    public partial class ArribaController
    {

        [HttpGet]
        public IActionResult GetTables()
        {
            return Ok(_arribaManagement.GetTables());
        }

        [HttpGet("allBasics")]
        public IActionResult GetAllBasics()
        {
            return Ok(_arribaManagement.GetTablesForUser(_telemetry, this.User));
        }

        [HttpGet("unloadAll")]
        public IActionResult GetUnloadAll()
        {
            if (!_arribaManagement.UnloadAllTableForUser(_telemetry, this.User))
                throw new ArribaAccessForbiddenException();

            return Ok($"All tables unloaded");
        }

        [HttpGet("table/{tableName}/unload")]
        public IActionResult GetUnloadTable(string tableName)
        {
            if (!_arribaManagement.UnloadTableForUser(tableName, _telemetry, this.User))
                throw new ArribaAccessForbiddenException();

            return Ok($"Table {tableName} unloaded");
        }

        [HttpPost("table")]
        public IActionResult PostCreateNewTable([Required] CreateTableRequest table)
        {
            _arribaManagement.CreateTableForUser(table, _telemetry, this.User);
            return CreatedAtAction(nameof(PostCreateNewTable), null);
        }

        [HttpPost("table/{tableName}/addcolumns")]
        public IActionResult PostAddColumn(string tableName, [FromBody, Required] IList<ColumnDetails> columnDetails)
        {
            _arribaManagement.AddColumnsToTableForUser(tableName, columnDetails, _telemetry, this.User);
            return CreatedAtAction(nameof(PostAddColumn), "Columns Added");
        }

        [HttpGet("table/{tableName}/save")]
        public IActionResult GetSaveTable(string tableName)
        {
            _arribaManagement.SaveTableForUser(tableName, _telemetry, this.User, VerificationLevel.Normal);
            return Ok("Saved");
        }

        [HttpGet("table/{tableName}/reload")]
        public IActionResult GetReloadTable(string tableName)
        {
            _arribaManagement.ReloadTableForUser(tableName, _telemetry, this.User);
            return Ok("Reloaded");
        }

        [HttpDelete("table/{tableName}")]
        [HttpGet("table/{tableName}/delete")]
        public IActionResult DeleteTable(string tableName)
        {
            _arribaManagement.DeleteTableForUser(tableName, _telemetry, this.User);
            return Ok("Deleted");
        }

        // {POST | GET} /table/foo?action=delete
        [HttpPost("table/{tableName}/deleterows")]
        [HttpGet("table/{tableName}/deleterows")]
        public IActionResult PostDeleteTableRows(string tableName, [FromQuery, Required] string q)
        {
            var result = _arribaManagement.DeleteTableRowsForUser(tableName, q, _telemetry, this.User);
            return Ok(result.Count);
        }

        [HttpPost("table/{tableName}/permissions/{scope}")]
        public IActionResult PostGrantTableAccess([FromQuery, Required] string tableName,
            [FromQuery, Required] PermissionScope scope,
            [FromBody, Required] SecurityIdentity identity)
        {
            _arribaManagement.GrantAccessForUser(tableName, identity, scope, _telemetry, this.User);
            return Ok("Granted");
        }

        [HttpDelete("table/{tableName}/permissions/{scope}")]
        public IActionResult DeleteRevokeTableAccess([FromQuery, Required] string tableName,
            [FromQuery, Required] PermissionScope scope,
            [FromBody, Required] SecurityIdentity identity)
        {
            _arribaManagement.RevokeAccessForUser(tableName, identity, scope, _telemetry, this.User);
            return Ok("Granted");
        }

    }

}
