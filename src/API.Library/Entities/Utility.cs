using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace API
{
    /// <summary>
    /// Collection of Utility methods
    /// </summary>
    public static class Utility
    {
      #region Properties

        #endregion

        #region Methods


        /// <summary>
        /// Generate the MD5 hash of the input parameter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5(string input)
        {

            using (var provider = MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(input)))
                    builder.Append(b.ToString("x2").ToLower());

                string hashMD5 = builder.ToString();

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

                return hashSHA256;
            }
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
                    return "data:" + mimeType + ";base64," + Convert.ToBase64String(byteArray);
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
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                if (string.IsNullOrEmpty(mimeType))
                {
                    return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                }
                else
                {
                    return "data:" + mimeType + ";base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                }
            }
            catch (Exception)
            {
                //Do not trow nor log. Instead, return null if data cannot be decoded
                return null;
            }
        }

        /// <summary>
        ///  Decode a base64 string into a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] DecodeBase64ToByteArray(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                if (data.Contains(";base64,"))
                {
                    // i.e. data:*/*;base64,cdsckdslfkdsfos
                    data = data.Split(new[] { ";base64," }, StringSplitOptions.None)[1];
                }

                return Convert.FromBase64String(data);
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
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                if (data.Contains(";base64,"))
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
            if (string.IsNullOrEmpty(inputUTF8))
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
            if (string.IsNullOrEmpty(inputBase64))
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
        /// check if file extension is allowed based on an array of passed in file extensions and the
        /// file extension of the uploaded file
        /// </summary>
        /// <returns></returns>
        public static bool IsFileExtensionAllowed(string[] allowedExtension, string fileExtension)
        {
            string doubleSlash = "\\";
            string singSlash = "\"";
            string dataFormat = fileExtension.Replace(doubleSlash, "").Replace(singSlash, "");
            bool res = false;
            for (var i = 0; i < allowedExtension.Length; i += 1)
            {
                if (allowedExtension[i].Trim().Equals(dataFormat))
                {
                    res = true;
                    break;
                }
            }
            return res;
        }
     

        /// <summary>
        /// Extension method to trim to whole minute
        /// </summary>
        /// <param name="date"></param>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public static DateTime TrimToMinute(this DateTime date, long ticks)
        {
            return new DateTime(date.Ticks - (date.Ticks % ticks));
        }
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }

            result = default(T);
            return false;
        }
 

        public static bool TryParseJson<T>(this string @this, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }

    
        public static decimal StopWatchToSeconds(Stopwatch sw)
        {
                return decimal.Round((decimal)sw.Elapsed.TotalMilliseconds / 1000, 3);
        }

        public static bool IsValidStatusCode(int code)
        {
            return Enum.IsDefined(typeof(HttpStatusCode), code);
        }
        #endregion

    }
}