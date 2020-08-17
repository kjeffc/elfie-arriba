using Arriba.Telemetry;

namespace Arriba.Configuration
{
    public class DefaultApplicationInsightsConfig : IApplicationInsightsConfiguration
    {
        public DefaultApplicationInsightsConfig(IArribaConfiguration config)
        {
            AppConfig = config;
        }

        public IArribaConfiguration AppConfig { get; }

        public string InstrumentationKey { get; set; }
    }
}
