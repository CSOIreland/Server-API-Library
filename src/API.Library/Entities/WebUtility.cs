using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace API
{
    /// <summary>
    /// Collection of Utility methods
    /// </summary>
    internal class WebUtility : IWebUtility
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        #region Properties

        #endregion

        #region Methods


        public WebUtility(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get a random MD5 hash code
        /// </summary>
        /// <param name="salsa"></param>
        /// <returns></returns>
        public string GetRandomMD5(string salsa)
        {
            return Utility.GetMD5(new Random().Next().ToString() + salsa + DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Get a random SHA256 hash code
        /// </summary>
        /// <param name="salsa"></param>
        /// <returns></returns>
        public string GetRandomSHA256(string salsa)
        {
            return Utility.GetSHA256(new Random().Next().ToString() + salsa + DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Get the IP Address of the current request
        /// </summary>
        /// <returns></returns>
        public string GetIP()
        {
            // Initialise
            string ipAddress = "";
            IPAddress ip;

            try
            {
                // check if this is a HTTP request
                if (_httpContextAccessor.HttpContext == null)
                {
                    // Look for the Network Host information
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    ipAddress = Convert.ToString(ipHostInfo.AddressList.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork));

                    Log.Instance.Info("IP Address (IPHostEntry): " + ipAddress);
                    return ipAddress;
                }

                // Look for the Server Variable HTTP_X_FORWARDED_FOR
                ipAddress = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (IPAddress.TryParse(ipAddress, out ip))
                {
                    Log.Instance.Info("IP Address (HTTP_X_FORWARDED_FOR): " + ipAddress);
                    return ipAddress;
                }

                //// Look for the Server Variable REMOTE_ADDR
                ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                if (IPAddress.TryParse(ipAddress, out ip))
                {
                    Log.Instance.Info("IP Address (REMOTE_ADDR): " + ipAddress);
                    return ipAddress;
                }

                throw new Exception("IP Address not found");
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }

        /// <summary>
        /// Get the User Agent from the Current Context
        /// </summary>
        public string GetUserAgent()
        {
            return _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString() == null ? "" : _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
        }

        /// <summary>
        /// Get the User's enviornment language from the http request Accept Languages
        /// C# code reverse engineered from JS https://github.com/opentable/accept-language-parser/
        /// </summary>
        /// <returns></returns>
        public string GetUserAcceptLanguage()
        {
            try
            {
                List<string> acceptLanguages = _httpContextAccessor.HttpContext.Request.Headers["Accept-Language"].ToList() == null ? new List<string>() { CultureInfo.CurrentCulture.Name } : _httpContextAccessor.HttpContext.Request.Headers["Accept-Language"].ToList();
                List<dynamic> outLanguages = new List<dynamic>();

                if (acceptLanguages.Count() == 0)
                {
                    return null;
                }

                foreach (string al in acceptLanguages)
                {

                    string[] bits = al.Split(';');
                    string[] ietf = bits[0].Split('-');
                    bool hasScript = ietf.Length == 3;
                    string q = "1.0";

                    if (bits.Count() > 1)
                    {
                        string[] innerBits = bits[1].Split('=');
                        q = innerBits.Count() > 1 ? innerBits[1] : "1.0";
                    }

                    outLanguages.Add(new
                    {
                        code = ietf[0],
                        script = hasScript && ietf.Count() > 1 ? ietf[1] : null,
                        region = hasScript && ietf.Count() > 2 ? ietf[2] : (ietf.Count() > 1 ? ietf[1] : null),
                        quality = Convert.ToDouble(q)
                    });
                }

                return outLanguages.OrderByDescending(x => x.quality).FirstOrDefault().code;
            }
            catch (Exception)
            {
                //Do not trow nor log. Instead, return null if language cannot be detected
                return null;
            }
        }
   #endregion
    }
}