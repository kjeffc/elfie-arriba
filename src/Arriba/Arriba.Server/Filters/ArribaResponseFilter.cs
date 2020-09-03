using Arriba.Server;
using Arriba.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arriba.Filters
{
    public class ArribaResultFilter : ResultFilterAttribute
    {
        public ArribaResultFilter()
        {
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Controller is ArribaController)
            {
                var currentResult = context.Result as ObjectResult;
                if (currentResult != null)
                {
                    var enveloped = new ArribaResponseEnvelope(IsSuccessfulResponse(currentResult), currentResult.Value);
                    currentResult.Value = enveloped;
                }
            }
        }

        private bool IsSuccessfulResponse(ObjectResult result)
        {
            return result.StatusCode >= 200 && result.StatusCode < 300;
        }
    }
}
