using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace API
{
    public interface ICleanser
    {

        /// <summary>
        /// This function runs a number of regex replace operations on the Json string of the parameters.
        /// The operations are:
        /// Remove possible HTML characters
        /// Remove double spaces from anywhere in the string
        /// Left trim anything between double quotes
        /// Right trim anything between double quotes
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public JObject Cleanse(dynamic parameters);


        //if its restful will be a list
        public IList<string> Cleanse(IList<string> parameters);

        /// <summary>
        /// Run Cleanse operations
        /// </summary>
        /// <param name="aValue"></param>
        /// <returns></returns>
        public string Cleanse(string aValue, bool htmlStrip = true);
    }
}
