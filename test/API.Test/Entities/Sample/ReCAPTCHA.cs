using API;
using System;

namespace Sample
{
    /// <summary>
    /// 
    /// </summary>
    public class YourReCAPTCHA
    {
        #region Methods
        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a JSONRPC_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic ValidateResponse(JSONRPC_API apiRequest)
        {
            // Initiate new object to handle the API Output
            JSONRPC_Output output = new JSONRPC_Output();

            try
            {
                Log.Instance.Debug("Validate the encoded response against the Google Server");
                // Use the ReCAPTCHA to validate the encoded Response against the Google Server
                bool isCaptchaValid = ReCAPTCHA.Validate(apiRequest.parameters.encodedResponse.ToString());

                if (isCaptchaValid)
                {
                    // If all goes well, then return the agreed success message with the front-end developer
                    output.data = "valid";
                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Valid ReCAPTCHA");
                }
                else
                {
                    // If an error/exception occurs, then return the error to the API
                    output.error = "invalid";
                    // Log an error message for your sake :-)
                    Log.Instance.Error("Invalid ReCAPTCHA");
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
        #endregion
    }
}
