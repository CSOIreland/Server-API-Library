# Enyim Memcached Client

This is a memcached client library for .NET migrated from [EnyimMemcached](https://github.com/enyim/EnyimMemcached).

## Configure
### The appsettings.json Without Authentication
```json
{
  "enyimMemcached": {
    "Servers": [
      {
        "Address": "memcached",
        "Port": 11211
      }
    ],
    "Transcoder": "MessagePackTranscoder"
  }
}
```
#### The appsettings.json With Authentication
```json
{
  "enyimMemcached": {
    "Servers": [
      {
        "Address": "memcached",
        "Port": 11211
      }
    ],
    "Authentication": {
      "Type": "Enyim.Caching.Memcached.PlainTextAuthenticator",
      "Parameters": {
        "zone": "",
        "userName": "username",
        "password": "password"
      }
    }
  }
}
```
### Startup.cs
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEnyimMemcached();
        // services.AddEnyimMemcached("enyimMemcached");
        // services.AddEnyimMemcached(Configuration);
        // services.AddEnyimMemcached(Configuration, "enyimMemcached");
        // services.AddEnyimMemcached(Configuration.GetSection("enyimMemcached"));
        // services.AddEnyimMemcached(options => options.AddServer("memcached", 11211));
    }
    
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    { 
        app.UseEnyimMemcached();
    }
}
```

## Example usage
### Use IMemcachedClient interface
```cs
public class HomeController : Controller
{
    private readonly IMemcachedClient _memcachedClient;
    private readonly IBlogPostService _blogPostService;

    public HomeController(IMemcachedClient memcachedClient, IBlogPostService blogPostService)
    {
        _memcachedClient = memcachedClient;
        _blogPostService = blogPostService;
    }

    public async Task<IActionResult> Index()
    {
        var cacheKey = "blogposts-recent";
        var cacheSeconds = 600;

        var posts = await _memcachedClient.GetValueOrCreateAsync(
            cacheKey,
            cacheSeconds,
            async () => await _blogPostService.GetRecent(10));

        return Ok(posts);
    }
}
```
### Use IDistributedCache interface
```cs
public class CreativeService
{
    private ICreativeRepository _creativeRepository;
    private IDistributedCache _cache;

    public CreativeService(
        ICreativeRepository creativeRepository,
        IDistributedCache cache)
    {
        _creativeRepository = creativeRepository;
        _cache = cache;
    }

    public async Task<IList<CreativeDTO>> GetCreatives(string unitName)
    {
        var cacheKey = $"creatives_{unitName}";
        IList<CreativeDTO> creatives = null;

        var creativesJson = await _cache.GetStringAsync(cacheKey);
        if (creativesJson == null)
        {
            creatives = await _creativeRepository.GetCreatives(unitName)
                    .ProjectTo<CreativeDTO>().ToListAsync();

            var json = string.Empty;
            if (creatives != null && creatives.Count() > 0)
            {
                json = JsonConvert.SerializeObject(creatives);
            }
            await _cache.SetStringAsync(
                cacheKey, 
                json, 
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));
        }
        else
        {
            creatives = JsonConvert.DeserializeObject<List<CreativeDTO>>(creativesJson);
        }

        return creatives;
    }
}
```
