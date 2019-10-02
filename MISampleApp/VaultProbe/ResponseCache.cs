namespace Azure.ServiceFabric.ManagedIdentity.Samples
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Primitive memory cache to store previously obtained responses.
    /// </summary>
    /// <remarks>
    /// Makes up for the absence of MemoryCache on .net Core.
    /// </remarks>
    public sealed class ResponseCache<TCachedItem>
    {
        private sealed class CacheEntry<TItem>
        {
            public TItem Item{ get; set; }

            public DateTimeOffset AddedOn { get; set; }

            public DateTimeOffset ExpiresOn { get; set; }
        }

        private Dictionary<string, CacheEntry<TCachedItem>> cache = new Dictionary<string, CacheEntry<TCachedItem>>();

        /// <summary>
        /// Caches the specified object until the specified time; if the item exists, its time to live in cache is extended. 
        /// </summary>
        /// <param name="key">Key under which to store the object.</param>
        /// <param name="item">Object being cached.</param>
        /// <param name="expiresOn">Timestamp up until which the item can be served.</param>
        public void AddOrUpdate(string key, TCachedItem item, DateTimeOffset expiresOn)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (DateTime.Now >= expiresOn) throw new ArgumentException($"'{nameof(expiresOn)}' may not occur in the past.");

            if (!Contains(key))
            {
                cache[key] = new CacheEntry<TCachedItem>
                {
                    Item = item,
                    AddedOn = DateTime.UtcNow,
                    ExpiresOn = expiresOn
                };
            }
            else
            {
                cache[key].AddedOn = DateTime.UtcNow;
                cache[key].ExpiresOn = expiresOn;
                cache[key].Item = item;
            }
        }

        public bool Contains(string key)
        {
            return !String.IsNullOrWhiteSpace(key) 
                && cache.ContainsKey(key);
        }

        public bool TryGetCachedItem(string key, out TCachedItem item)
        {
            item = GetCachedItem(key);

            return item != null;
        }

        public TCachedItem GetCachedItem(string key)
        {
            if (!Contains(key))
            {
                return default(TCachedItem);
            }

            var cacheEntry = cache[key];
            if (DateTime.Now < cacheEntry.ExpiresOn)
            {
                return cacheEntry.Item;
            }

            // evict if expired
            cache.Remove(key);

            return default(TCachedItem);
        }
    }
}
