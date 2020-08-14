using System;

namespace Arriba.Caching
{
    public interface IObjectCache : IDisposable
    {
        object Get(string key);
        
        bool Add(string key, object value, TimeSpan? timeToLive);

        object Remove(string key);
    }
}
