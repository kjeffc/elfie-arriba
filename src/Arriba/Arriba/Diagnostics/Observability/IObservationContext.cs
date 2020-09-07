using System;

namespace Arriba.Diagnostics.Observability
{
    public interface IObservationContext
    {
        Guid ContextId { get; }
        Guid SequenceId { get; }
        long TimeStamp { get; }
    }
}
