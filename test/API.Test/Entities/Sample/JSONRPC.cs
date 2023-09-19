using API;
using Newtonsoft.Json.Linq;

namespace Sample
{
    [AllowAPICall]
    /// <summary>
    /// 
    /// </summary>
    public class YourJSONRPC
    {
        #region Methods
        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a JSONRPC_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic YourExposedMethod(JSONRPC_API apiRequest)
        {
            // Initiate new object to handle the API Output
            JSONRPC_Output output = new JSONRPC_Output();

            // NB: You can access the userPrincipal object (if the Authentication is not Anonymous) to get all the user's AD details 

            // Run Validation

            // Run Business Logic
            try
            {
                // Let's just pretend all went well (for simulation only)
                bool isAllGood = true;

                if (isAllGood)
                {
                    // Test  as null
                    output.data = null;
                    // Test  as success
                    output.data = ApiServicesHelper.ApiConfiguration.Settings["API_SUCCESS"];
                    // Test data as string
                    output.data = "Place your data here, either as an Object, String, Int, JRaw... anything you need and the API will take care of converting and streaming anything";
                    // Test data as object
                    output.data = new { test = 123 };
                    // Test data as JRaw (json string)
                    output.data = new JRaw("{\"test\":123}");
                    // Test data as your input
                    output.data = apiRequest;
                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Place your debug log-message here");
                }
                else
                {
                    // If an error/exception occurs, then return the error to the API
                    output.error = "Place your error here, preferably as an Object.";
                    // Log an error message for your sake :-)
                    Log.Instance.Error("Place your error log-message here");
                }
            }
            catch (Exception e)
            {
                // If an error/exception occurs, then return the error to the API
                output.error = e;
                // Log an error message for your sake :-)
                Log.Instance.Error(e);
            }

            return output;
        }

        /// <summary>
        /// This method is internal and not exposed to the API but accessible within your Assemply project.
        /// </summary>
        internal static void YourInternalMethod()
        {
            // All your internal business logic goes here
        }

        /// <summary>
        /// This method is private and not exposed to the API and accessible only within your Class.
        /// </summary>
        private static void YourPrivateMethods()
        {
            // All your private business logic goes here
        }

        #endregion
    }
}
