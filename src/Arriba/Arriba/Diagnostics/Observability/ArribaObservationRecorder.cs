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

        public void FlushEvent()
        {
            if (_properties.Count() > 0)
            {
                string message = "";
                _properties.ToList().ForEach(x => message += x.Key + ": " + x.Value + "\n");

                FlushTrace(message);
                _properties.Clear();
            }
        }

        public void FlushMetrics()
        {
            if (_metrics.Count > 0)
            {
                string message = "";
                _metrics.ToList().ForEach(x => message += x.Key + ": " + x.Value + "\n");

                FlushTrace(message);
                _metrics.Clear();
            }
        }

        public void FlushTrace(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string message)
        {
            FlushTrace(message);
        }

        // TODO: make threadsafe
        public void RecordProperty(string name, string value)
        {
            _properties.Add(name, value);
        }

        // TODO: make threadsafe
        public void RecordMetric(string name, double value)
        {
            _metrics[name] = value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FlushMetrics();
                    FlushEvent();
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
