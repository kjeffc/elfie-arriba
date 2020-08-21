using System;

namespace Arriba.Serialization
{

    [Serializable]
    public class ArribaSerializationException : Exception
    {
        public ArribaSerializationException() { }
        public ArribaSerializationException(string message) : base(message) { }
        public ArribaSerializationException(string message, Exception inner) : base(message, inner) { }
        protected ArribaSerializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
