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
        }

        public bool API_CACHE_TRACE_ENABLED
        {
            get
            {
                return _CacheSettingsDelegate.CurrentValue.API_CACHE_TRACE_ENABLED;
            }
        }
        
    }
}
