// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Caching;

namespace Arriba.Caching
{
    public class MemoryCacheFactory : IObjectCacheFactory
    {
        public IObjectCache CreateCache(string name)
        {
            return new MemoryCacheImpl(name);
        }

        private class MemoryCacheImpl : IObjectCache
        {
            private readonly MemoryCache _cache;
            private readonly TimeSpan _maximumTimeToLive;

            public MemoryCacheImpl(string name)
            {
                _cache = new MemoryCache(name);
                _maximumTimeToLive = TimeSpan.FromDays(180);
            }

            public bool Add(string key, object value, TimeSpan? timeToLive)
            {
                return _cache.Add(key, value, this.CreatePolicyForValue(value, timeToLive));
            }
            
            public object Get(string key)
            {
                return _cache.Get(key);
            }

            public object Remove(string key)
            {
                return _cache.Remove(key);
            }

            public void Dispose()
            {
                _cache.Dispose();
            }

            private CacheItemPolicy CreatePolicyForValue(object value, TimeSpan? timeToLive)
            {
                var policy = new CacheItemPolicy();
                policy.SlidingExpiration = timeToLive ?? _maximumTimeToLive;

                // If the value is IDisposable, dispose of the item when it removed from the cache. 
                if (value is IDisposable)
                {
                    policy.RemovedCallback = (args) =>
                    {
                        ((IDisposable)args.CacheItem.Value).Dispose();
                    };
                }

                return policy;
            }
        }
    }
}
