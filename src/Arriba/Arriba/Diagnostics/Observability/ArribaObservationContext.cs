using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Arriba.Diagnostics.Observability
{
    public class ArribaObservationContext : IObservationContext
    {
        Guid _contextId;
        Guid _sequenceId;

        public Guid ContextId { get; }

        public Guid SequenceId => _sequenceId;

        public long TimeStamp { get; }

        public ArribaObservationContext()
        : this(Guid.NewGuid())
        { }

        public ArribaObservationContext(IObservationContext context)
        : this(context.ContextId, Guid.NewGuid())
        { }
        
        private ArribaObservationContext(Guid contextId)
        : this(contextId, contextId)
        { }

        // TODO: Change DateTime to NodaTime
        private ArribaObservationContext(Guid contextId, Guid sequenceId)
        {
            _contextId = contextId;
            _sequenceId = sequenceId;
            TimeStamp = DateTime.UtcNow.Ticks;
        }
    }
}
