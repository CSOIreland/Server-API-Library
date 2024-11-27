
namespace API
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException() : base()
        {
        }

        /// <summary>
        /// Create the exception with description
        /// </summary>
        /// <param name="message">Exception description</param>
        public ConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Create the exception with description and inner cause
        /// </summary>
        /// <param name="message">Exception description</param>
        /// <param name="innerException">Exception inner cause</param>
        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}