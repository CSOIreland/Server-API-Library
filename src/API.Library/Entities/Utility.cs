using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace API
{
    /// <summary>
    /// Collection of Utility methods
    /// </summary>
    public static class Utility
    {
        #region Properties

        /// <summary>
        /// Initiate Log4Net
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Set the IP Address
        /// </summary>
        public static string IpAddress = GetIP();

        /// <summary>
        /// Set the User Agent
        /// </summary>
        public static string UserAgent = GetUserAgent();

        #endregion

        #region Methods

        /// <summary>
        /// Generate the MD5 hash of the input parameter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5(string input)
        {
            Log.Instance.Info("Generate MD5 hash");
            Log.Instance.Info("Input string: " + input);

            using (var provider = MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(input)))
                    builder.Append(b.ToString("x2").ToLower());

                string hashMD5 = builder.ToString();

                Log.Instance.Info("Output hash: " + hashMD5);
                return hashMD5;
            }
        }

        /// <summary>
        /// Geberate the SHA256 hash of the input parameter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetSHA256(string input)
        {
            Log.Instance.Info("Generate SHA256 hash");
            Log.Instance.Info("Input string: " + input);

            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                string hashSHA256 = builder.ToString();

                Log.Instance.Info("Output hash: " + hashSHA256);
                return hashSHA256;
            }
        }

        /// <summary>
        /// Get the IP Address of the current request
        /// </summary>
        /// <returns></returns>
        private static string GetIP()
        {
            // Initialise
            string ipAddress = "";
            IPAddress ip;

            try
            {
                // Look for the Server Variable HTTP_X_FORWARDED_FOR
                ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (IPAddress.TryParse(ipAddress, out ip))
                {
                    Log.Instance.Info("IP Address (HTTP_X_FORWARDED_FOR): " + ipAddress);
                    return ipAddress;
                }

                // Look for the Server Variable REMOTE_ADDR
                ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                if (IPAddress.TryParse(ipAddress, out ip))
                {
                    Log.Instance.Info("IP Address (REMOTE_ADDR): " + ipAddress);
                    return ipAddress;
                }

                // Look for the Network Host information
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                ipAddress = Convert.ToString(ipHostInfo.AddressList.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork));

                Log.Instance.Info("IP Address (IPHostEntry): " + ipAddress);
                return ipAddress;
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
        private static string GetUserAgent()
        {
            return HttpContext.Current.Request.UserAgent.ToString();
        }

        /// <summary>
        /// Serialize to JSON ignoring looping references
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string JsonSerialize_IgnoreLoopingReference(dynamic input)
        {
            return JsonConvert.SerializeObject(input, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }

        /// <summary>
        /// Deserialize from JSON ignoring looping references
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static dynamic JsonDeserialize_IgnoreLoopingReference(string input)
        {
            return JsonConvert.DeserializeObject<dynamic>(input, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
        }

        /// <summary>
        /// Deserialize from JSON ignoring looping references
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static dynamic JsonDeserialize_IgnoreLoopingReference<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
        }

        /// <summary>
        /// Get the value from a custom config by sectionName and key
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetCustomConfig(string sectionName, string key)
        {
            NameValueCollection customConfig = (NameValueCollection)ConfigurationManager.GetSection(sectionName);
            if (!String.IsNullOrEmpty(customConfig[key].ToString()))
            {
                return customConfig[key].ToString();
            }
            else
                return "";
        }

        /// <summary>
        /// Get the value from a custom config by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetCustomConfig(string key)
        {
            return GetCustomConfig("appStatic", key);
        }

        /// <summary>
        /// Encode a byte array into a base64 string
        /// N.B. UFT8 in C# includes UTF16 too
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string EncodeBase64FromByteArray(byte[] byteArray, string mimeType = null)
        {
            try
            {
                if (byteArray == null)
                {
                    return null;
                }

                if (String.IsNullOrEmpty(mimeType))
                {
                    return System.Convert.ToBase64String(byteArray);
                }
                else
                {
                    return "data:" + mimeType + ";base64," + System.Convert.ToBase64String(byteArray);
                }
            }
            catch (Exception)
            {
                //Do not trow nor log. Instead, return null if data cannot be decoded
                return null;
            }
        }

        /// <summary>
        /// Encode a string into a base64 string
        /// N.B. UFT8 in C# includes UTF16 too
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string EncodeBase64FromUTF8(string data, string mimeType = null)
        {
            try
            {
                if (String.IsNullOrEmpty(data))
                {
                    return null;
                }

                if (String.IsNullOrEmpty(mimeType))
                {
                    return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
                }
                else
                {
                    return "data:" + mimeType + ";base64," + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
                }
            }
            catch (Exception)
            {
                //Do not trow nor log. Instead, return null if data cannot be decoded
                return null;
            }
        }

        /// <summary>
        /// Decode a base64 string into a UTF8 string
        /// N.B. UFT8 in C# includes UTF16 too
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string DecodeBase64ToUTF8(string data)
        {
            try
            {
                if (String.IsNullOrEmpty(data))
                {
                    return null;
                }

                if (data.ToLower().Contains(";base64,"))
                {
                    // i.e. data:*/*;base64,cdsckdslfkdsfos
                    data = data.Split(new[] { ";base64," }, StringSplitOptions.None)[1];
                }

                return Encoding.UTF8.GetString(Convert.FromBase64String(data));
            }
            catch (Exception)
            {
                //Do not trow nor log. Instead, return null if data cannot be decoded
                return null;
            }
        }

        /// <summary>
        /// Compress a UTF8 input string into Base64
        /// </summary>
        /// <param name="inputUTF8"></param>
        /// <returns></returns>
        public static string GZipCompress(string inputUTF8)
        {
            if (String.IsNullOrEmpty(inputUTF8))
            {
                return inputUTF8;
            }

            var byteInput = Encoding.UTF8.GetBytes(inputUTF8);

            using (var msInput = new MemoryStream(byteInput))
            using (var msOutput = new MemoryStream())
            {
                using (var stream = new GZipStream(msOutput, CompressionMode.Compress))
                {
                    msInput.CopyTo(stream);
                }

                return Convert.ToBase64String(msOutput.ToArray());
            }
        }

        /// <summary>
        /// Decompress a Base64 input string into UTF8
        /// </summary>
        /// <param name="inputBase64"></param>
        /// <returns></returns>
        public static string GZipDecompress(string inputBase64)
        {
            if (String.IsNullOrEmpty(inputBase64))
            {
                return inputBase64;
            }

            byte[] byteInput = Convert.FromBase64String(inputBase64);

            using (var msInput = new MemoryStream(byteInput))
            using (var msOutput = new MemoryStream())
            {
                using (var stream = new GZipStream(msInput, CompressionMode.Decompress))
                {
                    stream.CopyTo(msOutput);
                }

                return Encoding.UTF8.GetString(msOutput.ToArray());
            }
        }

        /// <summary>
        /// Get the User's enviornment language from the http request Accept Languages
        /// C# code reverse engineered from JS https://github.com/opentable/accept-language-parser/
        /// </summary>
        /// <returns></returns>
        public static string GetUserAcceptLanguage()
        {
            List<string> acceptLanguages = HttpContext.Current.Request.UserLanguages.ToList<string>();
            List<dynamic> outLanguages = new List<dynamic>();

            if (acceptLanguages.Count() == 0)
            {
                acceptLanguages.Add(CultureInfo.CurrentCulture.Name);
            }
            acceptLanguages.Add(CultureInfo.CurrentCulture.Name);
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
        #endregion
    }
}
