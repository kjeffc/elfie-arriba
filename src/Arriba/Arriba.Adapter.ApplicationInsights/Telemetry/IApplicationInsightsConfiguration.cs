using System;
using System.Collections.Generic;
using System.Text;
using Arriba.Configuration;

namespace Arriba.Telemetry
{
    public interface IApplicationInsightsConfiguration
    {
        IArribaConfiguration AppConfig { get; }

        string InstrumentationKey { get; }
    }
}
