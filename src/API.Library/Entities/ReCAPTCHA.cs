using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Policy;

namespace API
{
    /// <summary>
    /// Handle the Google ReCaptcha to validate human inputs in forms
    /// </summary>
    public static class ReCAPTCHA
    {
        #region Properties

        /// <summary>
        /// Flag to indicate if ReCAPTCHA is enabled
        /// </summary>
        //private static readonly bool API_RECAPTCHA_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_ENABLED"]);

        /// <summary>
        /// URL
        /// </summary>
       // private static readonly string API_RECAPTCHA_URL = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_URL"];

        /// <summary>
        /// private key
        /// </summary>
       // private static readonly string API_RECAPTCHA_PRIVATE_KEY = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_PRIVATE_KEY"];
        #endregion

        #region Methods
        /// <summary>
        /// Check if the ReCaptcha is enabled
        /// </summary>
        private static bool IsEnabled()
        {
            /// <summary>
            /// Flag to indicate if ReCAPTCHA is enabled
            /// </summary>
           bool API_RECAPTCHA_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_ENABLED"]);

            /// <summary>
            /// URL
            /// </summary>
            // private static readonly string API_RECAPTCHA_URL = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_URL"];

            /// <summary>
            /// private key
            /// </summary>
            // private static readonly string API_RECAPTCHA_PRIVATE_KEY = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_PRIVATE_KEY"];

            if (API_RECAPTCHA_ENABLED)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Validate the encoded response against the Google server
        /// </summary>
        /// <param name="encodedResponse"></param>
        /// <returns></returns>
        public static bool Validate(string encodedResponse)
        {
            /// <summary>
            /// Flag to indicate if ReCAPTCHA is enabled
            /// </summary>
            bool API_RECAPTCHA_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_ENABLED"]);

            /// <summary>
            /// URL
            /// </summary>
            string API_RECAPTCHA_URL = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_URL"];

            /// <summary>
            /// privates key
            /// </summary>
            string API_RECAPTCHA_PRIVATE_KEY = ApiServicesHelper.ApiConfiguration.Settings["API_RECAPTCHA_PRIVATE_KEY"];



            Log.Instance.Info("ReCAPTCHA Enabled: " + API_RECAPTCHA_ENABLED);
            Log.Instance.Info("ReCAPTCHA URL: " + API_RECAPTCHA_URL);
            Log.Instance.Info("ReCAPTCHA Private Key: ********"); // Hide API_RECAPTCHA_PRIVATE_KEY from logs

            // Skip the validation if not enabled
            if (!IsEnabled())
                return true;

            try
            {
                // Validate the response against the server
                var client = new HttpClient();

                var requestString = string.Format(API_RECAPTCHA_URL, API_RECAPTCHA_PRIVATE_KEY, encodedResponse);
                string responseString = "";
                
                using (HttpResponseMessage response = client.GetAsync(requestString).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        responseString = content.ReadAsStringAsync().Result;
                    }
                }



                var responseObject = Utility.JsonDeserialize_IgnoreLoopingReference<JObject>(responseString);
                var responseSuccess = (string)responseObject["success"];

                Log.Instance.Info("Server Response: " + responseString);

                if (responseSuccess.ToLowerInvariant() == "true")
                {
                    // All good and valid
                    Log.Instance.Info("Valid Encoded Response: " + encodedResponse);
                    return true;
                }
                else
                {
                    // Something went wrong
                    Log.Instance.Info("Invalid Encoded Response: " + encodedResponse);
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return false;
            }
        }

        #endregion
    }
}
