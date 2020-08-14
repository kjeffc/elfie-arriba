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
        private List<MonitorEventScope> _events = new List<MonitorEventScope>();
        private EventPublisherSource _eventSource;
        private IRequest _request;

        public RequestContext(IRequest request)
        {
            MonitorEventEntry defaults = new MonitorEventEntry()
            {
                Level = MonitorEventLevel.Verbose,
                Source = "HTTP",
                User = request.User.Identity.Name,
                Detail = null
            };

            _request = request;
            _eventSource = EventPublisher.CreateEventSource(defaults);
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
                return _events.GroupBy(e => e.Start.Name)
                                  .ToDictionary(e => e.Key, e => e.Sum(s => s.Stop == null ? s.CurrentRuntime : s.Stop.RuntimeMilliseconds), StringComparer.OrdinalIgnoreCase);
            }
        }

        public IDisposable Monitor(MonitorEventLevel level, string name, string type = null, string identity = null, object detail = null)
        {
            // TODO: Consider making detail evaluation lazy. 
            string detailValue = String.Empty;

            if (detail != null)
            {
                // Attempt to serialize  
                detailValue = ArribaConvert.ToJson(detail);
            }

            var evt = _eventSource.RaiseScope(level: level, entityType: type, entityIdentity: identity, name: name, detail: detailValue);
            _events.Add(evt);
            return evt;
        }
    }
}
