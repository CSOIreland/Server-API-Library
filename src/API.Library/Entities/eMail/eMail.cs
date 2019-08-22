using System;
using System.Net.Mail;
using System.Collections.Generic;
using System.Configuration;

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
        private static readonly bool API_EMAIL_ENABLED = Convert.ToBoolean(ConfigurationManager.AppSettings["API_EMAIL_ENABLED"]);

        /// <summary>
        /// Sender (it covers From and ReplyTo as well) 
        /// </summary>
        private static readonly string API_EMAIL_MAIL_SENDER = ConfigurationManager.AppSettings["API_EMAIL_MAIL_SENDER"];

        /// <summary>
        /// Server IP address
        /// </summary>
        private static readonly string API_EMAIL_SMTP_SERVER = ConfigurationManager.AppSettings["API_EMAIL_SMTP_SERVER"];

        /// <summary>
        /// Port number
        /// </summary>
        private static readonly int API_EMAIL_SMTP_PORT = Convert.ToInt32(ConfigurationManager.AppSettings["API_EMAIL_SMTP_PORT"]);

        /// <summary>
        /// Flag to indicate if SMTP authentication is required
        /// </summary>
        private static readonly bool API_EMAIL_SMTP_AUTHENTICATION = Convert.ToBoolean(ConfigurationManager.AppSettings["API_EMAIL_SMTP_AUTHENTICATION"]);

        /// <summary>
        /// Username if authentication is required
        /// </summary>
        private static readonly string API_EMAIL_SMTP_USERNAME = ConfigurationManager.AppSettings["API_EMAIL_SMTP_USERNAME"];

        /// <summary>
        /// Password if authentication is required
        /// </summary>
        private static readonly string API_EMAIL_SMTP_PASSWORD = ConfigurationManager.AppSettings["API_EMAIL_SMTP_PASSWORD"];

        /// <summary>
        /// Flag to indicate if SSL is required
        /// </summary>
        private static readonly bool API_EMAIL_SMTP_SSL = Convert.ToBoolean(ConfigurationManager.AppSettings["API_EMAIL_SMTP_SSL"]);

        /// <summary>
        /// Template Datetime Mask
        /// </summary>
        private static readonly string API_EMAIL_DATETIME_MASK = ConfigurationManager.AppSettings["API_EMAIL_DATETIME_MASK"];

        #endregion

        #region Methods
        /// <summary>
        /// Send an Email
        /// </summary>
        /// <returns></returns>
        public void Send()
        {
            Log.Instance.Info("Email Enabled: " + API_EMAIL_ENABLED);
            Log.Instance.Info("Email Sender: " + API_EMAIL_MAIL_SENDER);
            Log.Instance.Info("SMTP Server: " + API_EMAIL_SMTP_SERVER);
            Log.Instance.Info("SMTP Port: " + API_EMAIL_SMTP_PORT);
            Log.Instance.Info("SMTP Authentication: " + API_EMAIL_SMTP_AUTHENTICATION);
            Log.Instance.Info("SMTP Username: " + API_EMAIL_SMTP_USERNAME);
            Log.Instance.Info("SMTP Password: ********"); // Hide API_EMAIL_SMTP_PASSWORD from logs
            Log.Instance.Info("SMTP SSL: " + API_EMAIL_SMTP_SSL);

            if (!API_EMAIL_ENABLED)
            {
                return;
            }

            try
            {
                // Initiate new SMTP Client
                SmtpClient smtpClient = new SmtpClient(API_EMAIL_SMTP_SERVER, API_EMAIL_SMTP_PORT);
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
                this.ReplyToList.Add(new MailAddress(API_EMAIL_MAIL_SENDER));
                this.From = new MailAddress(API_EMAIL_MAIL_SENDER);
                this.Sender = new MailAddress(API_EMAIL_MAIL_SENDER);

                // Set the HTML body
                this.IsBodyHtml = true;

                // Send the mail
                smtpClient.Send(this);

                Log.Instance.Info("eMail sent");
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }

        /// <summary>
        /// Parse a Template located in Properties.Resources
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public string ParseTemplate(string template, List<eMail_KeyValuePair> eMail_KeyValuePair)
        {
            eMail_KeyValuePair.Add(new eMail_KeyValuePair() { key = "{datetime}", value = DateTime.Now.ToString(API_EMAIL_DATETIME_MASK) });
            eMail_KeyValuePair.Add(new eMail_KeyValuePair() { key = "{ip}", value = Utility.IpAddress });

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