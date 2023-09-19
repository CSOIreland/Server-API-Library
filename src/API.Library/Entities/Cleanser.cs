using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace API
{
    /// <summary>
    /// 
    /// </summary>
    public class Cleanser : ICleanser
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
        public JObject Cleanse(dynamic parameters)
        {
            return JObject.Parse(Cleanse(parameters.ToString()));
        }

        //if its restful will be a list
        public IList<string> Cleanse(IList<string> parameters)
        {
            List<string> cleanList = new List<string>();
            foreach (string s in parameters)
            {
                cleanList.Add(Cleanse(s));
            }
            return cleanList;
        }

    

        /// <summary>
        /// Run Cleanse operations
        /// </summary>
        /// <param name="aValue"></param>
        /// <returns></returns>
        public string Cleanse(string aValue, bool htmlStrip = true)
        {
            string pstring = "";

            if (aValue != null)
            {
                pstring = aValue;
                //Remove HTML parameters
                //pstring = Regex.Replace(aValue, @"<.*?>", "");
                if (htmlStrip)
                    pstring = aValue.Replace("<", "").Replace(">", "");

                //Remove double spaces
                pstring = Regex.Replace(pstring, @" {2,}", " ");

                // left trim anything between quotes
                pstring = Regex.Replace(pstring, "\"\\s+", "\"");

                //Right trim anything between quotes
                pstring = Regex.Replace(pstring, "\\s+\"", "\"");
            }

            return pstring;
        }
    }
}
