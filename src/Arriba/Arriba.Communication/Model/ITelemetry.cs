using Arriba.Monitoring;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Communication
{
    public interface ITelemetry
    {
        /// <summary>
        /// Begins a monitoring block for the request context. 
        /// </summary>
        /// <param name="level">Event Level</param>
        /// <param name="name">Name of the timing block.</param>
        /// <param name="detail">Detail message for event.</param>
        /// <returns>A disposable handle.</returns>
        IDisposable Monitor(MonitorEventLevel level, string name, string type = null, string identity = null, object detail = null);
    }
}
