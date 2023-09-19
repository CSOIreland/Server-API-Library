using FirebaseAdmin.Messaging;
using Microsoft.Identity.Client;
using System.Net.Mail;

namespace API
{
    /// <summary>
    /// Mail Message 
    /// </summary>
    public class eMail : MailMessage
    {
        #region Properties
        /// <summary>
        /// Swtich on/off the service
        /// </summary>
     //   private readonly bool API_EMAIL_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_ENABLED"]);

        /// <summary>
        /// NoReply email address
        /// </summary>
     //   private readonly string API_EMAIL_MAIL_NOREPLY = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_MAIL_NOREPLY"];

        /// <summary>
        /// Sender email address
        /// </summary>
    //    private readonly string API_EMAIL_MAIL_SENDER = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_MAIL_SENDER"];

        /// <summary>
        /// Server IP address
        /// </summary>
    //    private readonly string API_EMAIL_SMTP_SERVER = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_SERVER"];

        /// <summary>
        /// Port number
        /// </summary>
     //   private readonly string API_EMAIL_SMTP_PORT = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_PORT"];

        /// <summary>
        /// Flag to indicate if SMTP authentication is required
        /// </summary>
     //   private readonly bool API_EMAIL_SMTP_AUTHENTICATION = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_AUTHENTICATION"]);

        /// <summary>
        /// Username if authentication is required
        /// </summary>
    //    private readonly string API_EMAIL_SMTP_USERNAME = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_USERNAME"];

        /// <summary>
        /// Password if authentication is required
        /// </summary>
     //   private readonly string API_EMAIL_SMTP_PASSWORD = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_PASSWORD"];

        /// <summary>
        /// Flag to indicate if SSL is required
        /// </summary>
     //   private readonly bool API_EMAIL_SMTP_SSL = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_SSL"]);

        /// <summary>
        /// Template Datetime Mask
        /// </summary>
    //    private readonly string API_EMAIL_DATETIME_MASK = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_DATETIME_MASK"];

        #endregion
        public eMail()
        {
        }


    #region Methods
    /// <summary>
    /// Send an Email
    /// </summary>
    /// <returns></returns>
    public bool Send()
        {
            /// <summary>
            /// Swtich on/off the service
            /// </summary>
            bool API_EMAIL_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_ENABLED"]);
           
            /// <summary>
            /// NoReply email address
            /// </summary>
            string API_EMAIL_MAIL_NOREPLY = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_MAIL_NOREPLY"];

            /// <summary>
            /// Sender email address
            /// </summary>
            string API_EMAIL_MAIL_SENDER = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_MAIL_SENDER"];

            /// <summary>
            /// Server IP address
            /// </summary>
            string API_EMAIL_SMTP_SERVER = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_SERVER"];

            /// <summary>
            /// Port number
            /// </summary>
            string API_EMAIL_SMTP_PORT = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_PORT"];

            /// <summary>
            /// Flag to indicate if SMTP authentication is required
            /// </summary>
            bool API_EMAIL_SMTP_AUTHENTICATION = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_AUTHENTICATION"]);

            /// <summary>
            /// Username if authentication is required
            /// </summary>
            string API_EMAIL_SMTP_USERNAME = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_USERNAME"];

            /// <summary>
            /// Password if authentication is required
            /// </summary>
            string API_EMAIL_SMTP_PASSWORD = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_PASSWORD"];

            /// <summary>
            /// Flag to indicate if SSL is required
            /// </summary>
            bool API_EMAIL_SMTP_SSL = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_SMTP_SSL"]);

            /// <summary>
            /// Template Datetime Mask
            /// </summary>
            //    private readonly string API_EMAIL_DATETIME_MASK = ApiServicesHelper.ApiConfiguration.Settings["API_EMAIL_DATETIME_MASK"];

            Log.Instance.Info("Email Enabled: " + API_EMAIL_ENABLED);
            Log.Instance.Info("Email NoReply: " + API_EMAIL_MAIL_NOREPLY);
            Log.Instance.Info("Email Sender: " + API_EMAIL_MAIL_SENDER);
            Log.Instance.Info("SMTP Server: " + API_EMAIL_SMTP_SERVER);
            Log.Instance.Info("SMTP Port: " + API_EMAIL_SMTP_PORT);
            Log.Instance.Info("SMTP Authentication: " + API_EMAIL_SMTP_AUTHENTICATION);
            Log.Instance.Info("SMTP Username: " + API_EMAIL_SMTP_USERNAME);
            Log.Instance.Info("SMTP Password: ********"); // Hide API_EMAIL_SMTP_PAsSSWORD from logs
            Log.Instance.Info("SMTP SSL: " + API_EMAIL_SMTP_SSL);

            if (!API_EMAIL_ENABLED)
            {
                return false;
            }

            try
            {
                // Initiate new SMTP Client
                SmtpClient smtpClient = new SmtpClient(API_EMAIL_SMTP_SERVER, Convert.ToInt32(API_EMAIL_SMTP_PORT));
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                if (API_EMAIL_SMTP_AUTHENTICATION
                && !string.IsNullOrWhiteSpace(API_EMAIL_SMTP_USERNAME)
                && !string.IsNullOrWhiteSpace(API_EMAIL_SMTP_PASSWORD))
                {
                    // Use authentication if any
                    smtpClient.Credentials = new System.Net.NetworkCredential(API_EMAIL_SMTP_USERNAME, API_EMAIL_SMTP_PASSWORD);
                    smtpClient.UseDefaultCredentials = true;
                }

                if (API_EMAIL_SMTP_SSL)
                {
                    // Use SSL if any
                    smtpClient.EnableSsl = true;
                }

                // Override Sender, From, Reply To for security
                this.ReplyToList.Clear();
                this.ReplyToList.Add(new MailAddress(API_EMAIL_MAIL_NOREPLY));
                this.From = new MailAddress(API_EMAIL_MAIL_SENDER);
                this.Sender = new MailAddress(API_EMAIL_MAIL_SENDER);

                // Set the HTML body
                this.IsBodyHtml = true;

                // Send the mail
                smtpClient.Send(this);

                Log.Instance.Info("eMail sent");
                return true;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Parse a Template located in Properties.Resources
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public string ParseTemplate(string template, List<eMail_KeyValuePair> eMail_KeyValuePair)
        {

            Log.Instance.Info("eMail String-Template to parse: " + template);
            Log.Instance.Info("eMail List to parse: " + Utility.JsonSerialize_IgnoreLoopingReference(eMail_KeyValuePair));

            try
            {
                // Parse nodes
                foreach (var item in eMail_KeyValuePair)
                {
                    template = template.Replace(item.key, item.value);
                }

                return template;

            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }
        #endregion
    }

    public class eMail_KeyValuePair
    {
        #region Properties
        /// <summary>
        /// Key to parse
        /// </summary>
        public string key { get; set; }

        /// <summary>
        /// Value to parse
        /// </summary>
        public string value { get; set; }

        #endregion

        /// <summary>
        /// Initialise a blank one 
        /// </summary>
        public eMail_KeyValuePair()
        {
            key = null;
            value = null;
        }
    }

}
