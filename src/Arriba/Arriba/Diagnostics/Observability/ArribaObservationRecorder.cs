using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Arriba.Diagnostics.Observability
{
    public class ArribaObservationRecorder : IObservationRecorder
    {
        private bool disposedValue;

        public IObservationContext Context { get; } 
        private readonly IDictionary<string, string> _properties;
        private readonly IDictionary<string, double> _metrics;

        public ArribaObservationRecorder()
        {
            Context = new ArribaObservationContext();
            _properties = new Dictionary<string, string>();
            _metrics = new Dictionary<string, double>();
        }

        public void RecordEvent()
        {
            if (_properties.Count() > 0)
            {
                string message = "";
                _properties.ToList().ForEach(x => message += x.Key + ": " + x.Value + "\n");

                RecordTrace(message);
                _properties.Clear();
            }
        }

        public void RecordMetrics()
        {
            if (_metrics.Count > 0)
            {
                string message = "";
                _metrics.ToList().ForEach(x => message += x.Key + ": " + x.Value + "\n");

                RecordTrace(message);
                _metrics.Clear();
            }
        }

        public void RecordTrace(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string message)
        {
            RecordTrace(message);
        }

        // TODO: make threadsafe
        public void RememberProperty(string name, string value)
        {
            _properties.Add(name, value);
        }

        // TODO: make threadsafe
        public void RememberMetric(string name, double value)
        {
            _metrics[name] = value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RecordMetrics();
                    RecordEvent();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
