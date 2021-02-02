using System;
using System.Net;

namespace API
{
    /// <summary>
    /// Mapping class between APIs
    /// </summary>
    public static class Map
    {
        #region Properties
        #endregion

        #region Methods
        /// <summary>
        /// Map RESTful API to JSON-RPC API
        /// JSONRPC_API.parameters must be manipulated afterward
        /// </summary>
        /// <param name="RestfulApi"></param>
        /// <returns></returns>
        public static JSONRPC_API RESTful2JSONRPC_API(RESTful_API RestfulApi)
        {
            Log.Instance.Info("Map RESTful API to JSON-RPC API");

            return new JSONRPC_API
            {
                method = RestfulApi.method,
                parameters = RestfulApi.parameters,
                userPrincipal = RestfulApi.userPrincipal,
                ipAddress = RestfulApi.ipAddress,
                userAgent = RestfulApi.userAgent,
                httpGET = RestfulApi.httpGET,
                httpPOST = RestfulApi.httpPOST,
                sessionCookie = RestfulApi.sessionCookie
            };
        }

        /// <summary>
        /// Map JSON-RPC API to RESTful API
        /// RESTful_API.parameters must be manipulated afterward
        /// </summary>
        /// <param name="JsonRpcApi"></param>
        /// <returns></returns>
        public static RESTful_API JSONRPC2RESTful_API(JSONRPC_API JsonRpcApi)
        {
            Log.Instance.Info("Map JSON-RPC API to RESTful API");

            return new RESTful_API
            {
                method = JsonRpcApi.method,
                parameters = JsonRpcApi.parameters,
                userPrincipal = JsonRpcApi.userPrincipal,
                ipAddress = JsonRpcApi.ipAddress,
                userAgent = JsonRpcApi.userAgent,
                httpGET = JsonRpcApi.httpGET,
                httpPOST = JsonRpcApi.httpPOST,
                sessionCookie = JsonRpcApi.sessionCookie
            };
        }

        /// <summary>
        /// Map JSON-RPC Output to RESTful Output
        /// </summary>
        /// <param name="JsonRpcOutput"></param>
        /// <param name="mimeType"></param>
        /// <param name="statusCode4NoContent"></param>
        /// <returns></returns>
        public static RESTful_Output JSONRPC2RESTful_Output(JSONRPC_Output JsonRpcOutput, string mimeType = null, HttpStatusCode statusCode4NoContent = HttpStatusCode.NoContent)
        {
            Log.Instance.Info("Map JSON-RPC Output to RESTful Output");

            if (JsonRpcOutput == null)
            {
                return null;
            }
            else if (JsonRpcOutput.error != null)
            {
                return new RESTful_Output
                {
                    mimeType = null,
                    statusCode = HttpStatusCode.InternalServerError,
                    response = JsonRpcOutput.error
                };
            }
            else if (JsonRpcOutput.data == null)
            {
                return new RESTful_Output
                {
                    mimeType = null,
                    statusCode = statusCode4NoContent,
                    response = JsonRpcOutput.data
                };
            }
            else if (mimeType == "application/base64")
            {
                try
                {
                    string data = JsonRpcOutput.data.ToString();
                    if (data.Contains(";base64,"))
                    {
                        var base64Splits = data.Split(new[] { ";base64," }, StringSplitOptions.None);
                        var dataSplits = base64Splits[0].Split(new[] { "data:" }, StringSplitOptions.None);

                        // Override MimeType & Data
                        mimeType = dataSplits[1];
                        JsonRpcOutput.data = base64Splits[1];
                    }

                    return new RESTful_Output
                    {
                        mimeType = mimeType,
                        statusCode = HttpStatusCode.OK,
                        response = Utility.DecodeBase64ToByteArray(JsonRpcOutput.data)
                    };
                }
                catch (Exception)
                {
                    //Do not trow nor log. Instead, return whatever it is
                    return new RESTful_Output
                    {
                        mimeType = mimeType,
                        statusCode = HttpStatusCode.OK,
                        response = JsonRpcOutput.data
                    };
                }
            }
            else
            {
                return new RESTful_Output
                {
                    mimeType = mimeType,
                    statusCode = HttpStatusCode.OK,
                    response = JsonRpcOutput.data
                };
            }
        }

        /// <summary>
        /// Map RESTful Output to JSON-RPC Output
        /// </summary>
        /// <param name="restfulOutput"></param>
        /// <returns></returns>
        public static JSONRPC_Output RESTful2JSONRPC_Output(RESTful_Output restfulOutput)
        {
            Log.Instance.Info("Map RESTful Output to JSON-RPC Output");

            if (restfulOutput == null)
            {
                return null;
            }
            else if (restfulOutput.statusCode == HttpStatusCode.OK || restfulOutput.statusCode == HttpStatusCode.NoContent)
            {
                return new JSONRPC_Output
                {
                    data = restfulOutput.response
                };
            }
            else
            {
                return new JSONRPC_Output
                {
                    error = restfulOutput.response
                };
            }
        }
        #endregion
    }
}
