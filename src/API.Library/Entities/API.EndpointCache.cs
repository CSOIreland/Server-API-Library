using API;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Thread-safe API endpoint cache.
/// </summary>
public static class EndpointCache
{
    // Dictionary of allowed class/method 
    private static ConcurrentDictionary<string, bool> _allowedClasses = new ConcurrentDictionary<string, bool>();

    // List of assemblies 
    private static List<Assembly> _configuredAssemblies = new List<Assembly>();

    /// <summary>
    /// Configure assemblies from Program.cs (optional).
    /// If not called or empty, defaults to all loaded assemblies.
    /// </summary>
    public static void SetAssemblies(IEnumerable<Assembly>? assemblies = null)
    {
        _configuredAssemblies = (assemblies == null || !assemblies.Any())
                ? AppDomain.CurrentDomain.GetAssemblies().ToList()
                : assemblies.ToList();

        Reload();
    }


    /// <summary>
    /// Reloads the cache from specified assemblies. If null, uses all loaded assemblies.
    /// </summary>
    public static void Reload()
    {

        var assembliesToScan = _configuredAssemblies.Any() ? _configuredAssemblies : AppDomain.CurrentDomain.GetAssemblies().ToList();


        //Ensure we have unique entries (overloaded methods will appear multiple times otherwise and fail in the Dictionary)
        var ats = assembliesToScan
             .SelectMany(a => a.GetTypes())
             .Where(t => t.IsClass && t.IsDefined(typeof(AllowAPICall), inherit: false))
             .SelectMany(t => t.GetMethods(
                 BindingFlags.Public |
                 BindingFlags.Instance |
                 BindingFlags.Static |
                 BindingFlags.DeclaredOnly
             ).Select(m => t.FullName + '.' + m.Name)).Distinct().ToList<string>() ;

        _allowedClasses= new ConcurrentDictionary<string, bool>(
            ats.Select(m => new KeyValuePair<string, bool>(m, true))
        );

  
    }

    public static bool findApi(string Key)
    {
        if(_allowedClasses.IsEmpty)
        {
            Reload();        
        }

        return _allowedClasses.ContainsKey(Key);
    }


    public static List<Assembly> GetListAssemblies()
    {
        if (_allowedClasses.IsEmpty)
        {
            Reload();
        }

        return _configuredAssemblies;
    }
}