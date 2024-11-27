using Microsoft.Extensions.Options;

namespace API
{
   public class CacheConfig : ICacheConfig
    {
        internal static IOptionsMonitor<CacheSettings> _CacheSettingsDelegate;

        public CacheConfig(IOptionsMonitor<CacheSettings> CacheSettingsDelegate) {
            _CacheSettingsDelegate = CacheSettingsDelegate;

        }
        public string API_MEMCACHED_SALSA
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_MEMCACHED_SALSA;
            }
        }
        public uint API_MEMCACHED_MAX_VALIDITY
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_MEMCACHED_MAX_VALIDITY;
            }
        }
        public uint API_MEMCACHED_MAX_SIZE
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_MEMCACHED_MAX_SIZE;
            }
        }
        public bool API_MEMCACHED_ENABLED
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_MEMCACHED_ENABLED;
            }
            set{
                _CacheSettingsDelegate.CurrentValue.API_MEMCACHED_ENABLED = value;
            }
        }

        public bool API_CACHE_TRACE_ENABLED
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_TRACE_ENABLED;
            }
        }

        public bool API_CACHE_LOCK_ENABLED
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_ENABLED;
            }
        }

        public int API_CACHE_LOCK_POLL_INTERVAL
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_POLL_INTERVAL;
            }
        }

        public string API_CACHE_LOCK_PREFIX
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_PREFIX;
            }
        }

        public int API_CACHE_LOCK_MAX_TIME
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_MAX_TIME;
            }
        }

    }
}
