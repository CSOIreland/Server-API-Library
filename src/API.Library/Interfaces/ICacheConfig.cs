namespace API
{
    public interface ICacheConfig
    {
        string API_MEMCACHED_SALSA { get;  }
        uint API_MEMCACHED_MAX_VALIDITY { get;  }
        uint API_MEMCACHED_MAX_SIZE { get; }
        bool API_MEMCACHED_ENABLED { get; set; }
        bool API_CACHE_TRACE_ENABLED { get; }
        
    }
}
