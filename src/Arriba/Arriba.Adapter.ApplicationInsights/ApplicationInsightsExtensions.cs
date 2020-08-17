using System.Diagnostics;
using Arriba.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.TraceListener;
using Microsoft.Extensions.DependencyInjection;

namespace Arriba
{
    public static class ApplicationInsightsExtensions
    {
        public static void AddApplicationInsights(this IServiceCollection services, IApplicationInsightsConfiguration config)
        {
            if (!string.IsNullOrWhiteSpace(config?.InstrumentationKey))
            {
                Trace.WriteLine("Enabling Application Insights Telemetry");

                services.AddApplicationInsightsTelemetry((options) =>
                {
                    options.ApplicationVersion = config.AppConfig.ApplicationVersion;
                    options.InstrumentationKey = config.InstrumentationKey;
                });

                services.AddSingleton<ITelemetryInitializer>(new ReportServiceNameProcessor(config.AppConfig.ServiceName));
            }
        }

        public static void UseAppInsightsTraceListener(this IApplicationInsightsConfiguration config)
        {
            Trace.WriteLine("Registering Application Insights trace listener");
            var listener = new ApplicationInsightsTraceListener(config.InstrumentationKey);
            Trace.Listeners.Add(listener);
        }
    }
}
