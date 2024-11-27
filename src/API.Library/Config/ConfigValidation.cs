namespace API;
    public static class ConfigValidation
    {
        public static bool cacheSalsaValidation(CacheSettings config)
        {
            if (config.API_MEMCACHED_ENABLED)
            {
                if (string.IsNullOrEmpty(config.API_MEMCACHED_SALSA))
                {
                    throw new ConfigurationException("Memcache salsa must not be null");
                }
            }
            return true;
        }

        public static bool cacheLockValidation(CacheSettings config)
        {
            if (config.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_ENABLED)
            {
                if (string.IsNullOrEmpty(config.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_PREFIX))
                {
                    throw new ConfigurationException("Cache lock prefix must not be null");
                }
                if (config.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_MAX_TIME <= 0)
                {
                    throw new ConfigurationException("Cache lock max time must be greater than 0");
                }
                if (config.API_CACHE_LOCK_SETTINGS.API_CACHE_LOCK_POLL_INTERVAL <= 0)
                {
                    throw new ConfigurationException("Cache lock poll interval must be greater than 0");
                }
            }
            return true;
        }
    }

