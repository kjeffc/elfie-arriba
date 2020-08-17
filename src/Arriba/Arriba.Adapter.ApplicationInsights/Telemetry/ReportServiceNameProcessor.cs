using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Arriba.Telemetry
{
    public class ReportServiceNameProcessor : ITelemetryInitializer
    {
        private readonly string _serviceName;
        
        public ReportServiceNameProcessor(string serviceName)
        {
            _serviceName = serviceName;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is null)
            {
                throw new System.ArgumentNullException(nameof(telemetry));
            }

            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                telemetry.Context.Cloud.RoleName = _serviceName;
            }
        }
    }
}
