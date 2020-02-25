using API;
using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace Sample
{
    public class YourEmail
    {
        #region Methods
        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a JSONRPC_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic SendEmail(JSONRPC_API apiRequest)
        {
            // Initiate new object to handle the API Output
            JSONRPC_Output output = new JSONRPC_Output();

            try
            {
                Log.Instance.Debug("Build the Mail proprieties");

                // Create a new instance of the eMail object
                eMail eMail = new eMail();

                // Set the Subject
                eMail.Subject = "Place here the email Subject";

                // Add the BCC list (if required)
                if (apiRequest.parameters.bcc != null)
                {
                    eMail.Bcc.Add(new MailAddress(apiRequest.parameters.bcc.ToString()));
                }
                // Add the CC list (if required)
                if (apiRequest.parameters.cc != null)
                {
                    eMail.CC.Add(new MailAddress(apiRequest.parameters.cc.ToString()));
                }
                // Add the TO list
                if (apiRequest.parameters.to != null)
                {
                    eMail.To.Add(new MailAddress(apiRequest.parameters.to.ToString()));
                }

                // Add the body
                // eMail.Body = "Place here the body of your email";

                // Or create e list of Key-Value pairs to parse
                var listToParse = new List<eMail_KeyValuePair>();

                listToParse.Add(new eMail_KeyValuePair() { key = "{title}", value = "Test API" });
                listToParse.Add(new eMail_KeyValuePair() { key = "{subtitle}", value = "eMail from the Test API" });
                listToParse.Add(new eMail_KeyValuePair() { key = "{website_name}", value = "domain.extension" });
                listToParse.Add(new eMail_KeyValuePair() { key = "{website_url}", value = "https://domain.extension" });
                listToParse.Add(new eMail_KeyValuePair() { key = "{body}", value = "Place here the body of your email" });

                eMail.Body = eMail.ParseTemplate(Properties.Resources.eMail, listToParse);

                //Fire & Forget the email
                if (eMail.Send())
                {
                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Email fired");
                    // If all goes well, then return the agreed success message with the front-end developer
                    output.data = JSONRPC.success;
                }
                else
                {
                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Something wrong with firing the email?!");
                    // If all goes well, then return the agreed success message with the front-end developer
                    output.error = "Something wrong with firing the email?!";
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
