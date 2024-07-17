
using System.Collections.Concurrent;
using System.Reflection;

namespace API
{
    /// <summary>
    /// Static dictionarys for attributes to reduce need for reflection
    /// </summary>
    public static class AttributeDictionary
    {

        public static ConcurrentDictionary<string, RuntimeMethodHandle> AllowedAPIDictionary = new ConcurrentDictionary<string, RuntimeMethodHandle>();

        public static ConcurrentDictionary<string, RuntimeMethodHandle> DictMethodAttributeValue = new ConcurrentDictionary<string, RuntimeMethodHandle>();

        public static ConcurrentDictionary<string, RuntimeMethodHandle> DictMethodHasAttribute = new ConcurrentDictionary<string, RuntimeMethodHandle>();

    }
}