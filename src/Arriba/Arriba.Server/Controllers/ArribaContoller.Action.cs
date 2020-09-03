using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Arriba.ParametersCheckers;

namespace Arriba.Controllers
{

    /// <summary>
    /// For retro-compatibility with the routes defined in the V1
    /// </summary>
    public partial class ArribaController
    {

        private const string DeleteActionQueryParameter = "q";
        private const string DeleteAction = "delete";
        private const string SelectAction = "select";
        private const string DistinctAction = "distinct";
        private const string AggregateAction = "aggregate";

        // {POST | GET} /table/foo?action=delete
        // {POST | GET} /table/foo?action=select
        // {POST | GET} /table/foo?action=distinct
        // {POST | GET} /table/foo?action=aggregate        
        [HttpPost("table/{tableName}")]
        [HttpGet("table/{tableName}")]
        public async Task<IActionResult> SelectActionAsync(string tableName, [FromQuery, Required] string action)
        {
            var queryString = await GetParametersFromQueryStringAndBody(Request);
            queryString.ThrowIfNullOrEmpty(nameof(queryString));

            switch (action)
            {
                case DeleteAction: return PostDeleteTableRows(tableName, queryString[DeleteActionQueryParameter]);
                case SelectAction: return await GetTable(tableName);
                case DistinctAction: return await GetTableDistinct(tableName);
                case AggregateAction: return await GetTableAggregate(tableName);
            }

            return BadRequest($"Action {action} not supported");
        }
    }
}
