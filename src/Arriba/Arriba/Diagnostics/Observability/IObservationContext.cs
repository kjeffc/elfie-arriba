using System;

namespace Arriba.Diagnostics.Observability
{
    // TODO: Address primitive obsession
    public interface IObservationContext
    {
        Guid ContextId { get; }
        Guid SequenceId { get; }
        long TimeStamp { get; }
    }
}
