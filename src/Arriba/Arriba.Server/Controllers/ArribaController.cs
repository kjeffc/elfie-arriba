using Arriba.Communication.Server.Application;
using Arriba.Filters;
using Arriba.Monitoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace Arriba.Controllers
{
    [Route("api/Arriba")]
    [ApiController]
    [ArribaResultFilter]
    public partial class ArribaController : ControllerBase
    {
        private readonly IArribaQueryServices _arribaQuery;
        private readonly IArribaManagementService _arribaManagement;
        private readonly ITelemetry _telemetry;

        public ArribaController(IArribaManagementService arribaManagement, IArribaQueryServices arribaQuery, ITelemetry telemetry)
        {
            _arribaManagement = arribaManagement;
            _arribaQuery = arribaQuery;
            _telemetry = telemetry;
        }

        private async Task<NameValueCollection> GetParametersFromQueryStringAndBody(HttpRequest request)
        {
            NameValueCollection parameters = new NameValueCollection();

            // Read parameters from query string
            parameters.Add(HttpUtility.ParseQueryString(request.QueryString.Value));

            // Read parameters from body (these will override ones from the query string)
            using var sr = new StreamReader(request.Body);
            var queryStringInBody = await sr.ReadToEndAsync();
            parameters.Add(HttpUtility.ParseQueryString(queryStringInBody));

            return parameters;
        }
    }
}
