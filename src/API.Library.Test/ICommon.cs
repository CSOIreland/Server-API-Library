using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace API.Library.Test
{
    public interface ICommon
    {
        string MaskParameters(string input);
    }

    public class Common : ICommon
    {
        public string MaskParameters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }
            // Init the output
            string output = input;

            // Loop trough the parameters to mask API_MASK_PARAMETERS
            foreach (var param in ApiServicesHelper.ApiConfiguration.Settings["API_MASK_PARAMETERS"].Split(','))
            {
                // https://stackoverflow.com/questions/171480/regex-grabbing-values-between-quotation-marks
                Log.Instance.Info("Masked parameter: " + param);
                output = Regex.Replace(output, "\"" + param + "\"\\s*:\\s*\"(.*?[^\\\\])\"", "\"" + param + "\": \"********\"", RegexOptions.IgnoreCase);
            }

            return output;
        }
    }
}
