// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Monitoring;

namespace Arriba.Communication
{
    internal class RequestContext : IRequestContext
    {
        private readonly ITelemetry _telemetry;

        private IRequest _request;

        public RequestContext(IRequest request)
        {
            _telemetry = new Telemetry(MonitorEventLevel.Verbose, "HTTP", null);
            _request = request;
        }

        public IRequest Request
        {
            get
            {
                return _request;
            }
        }

        public IDictionary<string, double> TraceTimings
        {
            get
            {
                return _telemetry.MonitorEvents().GroupBy(e => e.Start.Name)
                                  .ToDictionary(e => e.Key, e => e.Sum(s => s.Stop == null ? s.CurrentRuntime : s.Stop.RuntimeMilliseconds), StringComparer.OrdinalIgnoreCase);
            }
        }

        // This fails when attempting to serialize
        public IDisposable Monitor(MonitorEventLevel level, string name, string type = null, string identity = null, object detail = null)
        {
            return _telemetry.Monitor(level, name, type, identity, detail);
        }

        public IList<MonitorEventScope> MonitorEvents() => _telemetry.MonitorEvents();

    }
}
