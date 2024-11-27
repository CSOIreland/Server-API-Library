namespace API
{
    public interface ICacheConfig
    {
        string API_MEMCACHED_SALSA { get;  }
        uint API_MEMCACHED_MAX_VALIDITY { get;  }
        uint API_MEMCACHED_MAX_SIZE { get; }
        bool API_MEMCACHED_ENABLED { get; set; }
        bool API_CACHE_TRACE_ENABLED { get; }
        int API_CACHE_LOCK_POLL_INTERVAL { get; }
        string API_CACHE_LOCK_PREFIX { get; }
        int API_CACHE_LOCK_MAX_TIME { get;}

        bool API_CACHE_LOCK_ENABLED { get; }
    }
}
