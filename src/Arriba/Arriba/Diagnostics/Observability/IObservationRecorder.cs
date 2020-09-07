using System;
namespace Arriba.Diagnostics.Observability
{
    public interface IObservationRecorder : IDisposable
    {
        IObservationContext Context { get; }

        void RecordTrace(string message);
        void RecordEvent();
        void RecordMetrics();
        void RememberProperty(string name, string value);
        void RememberMetric(string name, double value);

    }
}
