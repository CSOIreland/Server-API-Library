using System.Reflection;
using log4net;

namespace API
{
    /// <summary>
    /// Static implementation of the Log4Net
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Initiate Log4Net 
        /// </summary>
        internal static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Return the Instace of the static class
        /// </summary>
        static public ILog Instance
        {
            get
            {
                return log;
            }
        }

    }
}