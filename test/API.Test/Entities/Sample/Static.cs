using API;
using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace Sample
{
    [AllowAPICall]

    /// <summary>
    /// 
    /// </summary>
    public class YourStatic
    {
        #region Methods
        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a Static_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic YourExposedMethod(Static_API apiRequest)
        {
            // Initiate new object to handle the API Output
            Static_Output output = new Static_Output();

            Log.Instance.Debug("Place your debug log-message here");

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
                    output.response = null;
                    output.mimeType = "text/plain";
                    output.statusCode = HttpStatusCode.OK;

                    // Test  as null
                    output.response = ApiServicesHelper.ApiConfiguration.Settings["API_SUCCESS"]; ;
                    output.mimeType = "text/plain";
                    output.statusCode = HttpStatusCode.OK;

                    // Test data as string
                    output.response = "Place your data here, either as an Object, String, Int, JRaw... anything you need and the API will take care of converting and streaming anything";
                    output.mimeType = "text/plain";
                    output.statusCode = HttpStatusCode.OK;

                    // Test data as object
                    output.response = Utility.JsonSerialize_IgnoreLoopingReference(new { test = 123 });
                    output.mimeType = "application/json";
                    output.statusCode = HttpStatusCode.OK;

                    // Test data as JRaw (json string)
                    output.response = Utility.JsonSerialize_IgnoreLoopingReference(new JRaw("{\"test\":123}"));
                    output.mimeType = "application/json";
                    output.statusCode = HttpStatusCode.OK;

                    // Test data as your input (json string)
                    output.response = Utility.JsonSerialize_IgnoreLoopingReference(apiRequest);
                    output.mimeType = "application/json";
                    output.statusCode = HttpStatusCode.OK;

                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Place your debug log-message here");
                }
                else
                {
                    // Set the HTTP Status Code accordingly
                    output.statusCode = HttpStatusCode.BadRequest;
                    // The response is optional an can be used to send a message together with the status code
                    output.response = "Place your (optional) error message here";

                    // Log an error message for your sake :-)
                    Log.Instance.Error("Place your error log-message here");
                }
            }
            catch (Exception e)
            {
                // Set the HTTP Status Code accordingly
                output.statusCode = HttpStatusCode.InternalServerError;
                // The response is optional an can be used to send a message together with the status code
                output.response = e.Message;

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
