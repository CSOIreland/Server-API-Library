
using System.Reflection;

namespace API
{
    /// <summary>
    /// Static implementation of the Log4Net
    /// </summary>
    public static class AttributeDictionary
    {
        /// <summary>
        /// Initiate Log4Net 
        /// </summary>
        internal static Dictionary<string, RuntimeMethodHandle> AllowedAPIDictionary = new Dictionary<string, RuntimeMethodHandle>();


        internal static Dictionary<string, RuntimeMethodHandle> DictMethodAttributeValue = new Dictionary<string, RuntimeMethodHandle>();


        internal static Dictionary<string, RuntimeMethodHandle> DictMethodHasAttribute = new Dictionary<string, RuntimeMethodHandle>();


    }
}