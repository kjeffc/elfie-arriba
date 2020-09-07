using System;
namespace Arriba.Diagnostics.Observability
{
    public interface IObservationRecorder : IDisposable
    {
        IObservationContext Context { get; }

        void FlushTrace(string message);
        void FlushEvent();
        void FlushMetrics();
        void RecordProperty(string name, string value);
        void RecordMetric(string name, double value);

    }
}
