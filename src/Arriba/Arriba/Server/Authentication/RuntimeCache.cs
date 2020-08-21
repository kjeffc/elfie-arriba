// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Arriba.Caching;

namespace Arriba.Server.Authentication
{
    /// <summary>
    /// Simple runtime object cache. 
    /// </summary>
    public class RuntimeCache : IDisposable
    {
        private IObjectCache _cache;

        public RuntimeCache(IObjectCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Gets an existing cache item for the specified key, if it does not exist a cache item is added by running the specified production. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Cache item key.</param>
        /// <param name="production">Function to call in the case of cache miss.</param>
        /// <param name="timeToLive">Time for the cache item to live.</param>
        /// <returns>Cache item.</returns>
        public T GetOrAdd<T>(string key, Func<T> production, TimeSpan? timeToLive = null)
        {
            object value = _cache.Get(key);

            if (value == null)
            {
                value = production();
                _cache.Add(key, value, timeToLive);
            }

            return (T)value;
        }

        /// <summary>
        /// Removes and returns the specified cache item. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Cache item key.</param>
        /// <returns>Cache item.</returns>
        public object Remove(string key)
        {
            return _cache.Remove(key);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
