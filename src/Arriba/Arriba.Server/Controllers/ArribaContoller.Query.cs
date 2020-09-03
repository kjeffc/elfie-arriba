using Arriba.ParametersCheckers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Arriba.Controllers
{
    public partial class ArribaController
    {
        private async Task<NameValueCollection> GetParameters()
        {
            var parameters = await GetParametersFromQueryStringAndBody(Request);
            parameters.ThrowIfNullOrEmpty(nameof(parameters));
            return parameters;
        }

        [HttpPost("table/{tableName}/select")]
        [HttpGet("table/{tableName}/select")]
        public async Task<IActionResult> GetTable(string tableName)
        {
            var parameters = await GetParameters();
            return Ok(_arribaQuery.QueryTableForUser(tableName, parameters, _telemetry, User));
        }

        [HttpPost("table/{tableName}/distinct")]
        [HttpGet("table/{tableName}/distinct")]
        public async Task<IActionResult> GetTableDistinct(string tableName)
        {
            var parameters = await GetParameters();
            return Ok(_arribaQuery.DistinctQueryTableForUser(tableName, parameters, _telemetry, User));
        }

        [HttpPost("table/{tableName}/aggregate")]
        [HttpGet("table/{tableName}/aggregate")]
        public async Task<IActionResult> GetTableAggregate(string tableName)
        {
            var parameters = await GetParameters();
            return Ok(_arribaQuery.AggregateQueryTableForUser(tableName, parameters, _telemetry, User));
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> GetSuggestion()
        {
            var parameters = await GetParameters();
            return Ok(_arribaQuery.IntelliSenseTableForUser(parameters, _telemetry, User));
        }

        [HttpGet("allCount")]
        public async Task<IActionResult> GetAllCount()
        {
            var parameters = await GetParameters();
            return Ok(_arribaQuery.AllCountForUser(parameters, _telemetry, User));
        }
    }
}
