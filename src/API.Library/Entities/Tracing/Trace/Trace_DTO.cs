namespace API
{
    /// <summary>
    /// DTO for Trace Create
    /// </summary>
    public class Trace
    {
        #region Properties
        /// <summary>
        /// Trace Method
        /// </summary>
        public string TrcMethod { get; set; }

        /// <summary>
        /// Trace parameters
        /// </summary>
        public string TrcParams { get; set; }

        /// <summary>
        /// Trace ip address
        /// </summary>
        public string TrcIp { get; set; }

        /// <summary>
        /// Trace useragent string
        /// </summary>
        public string TrcUseragent { get; set; }

        /// <summary>
        /// account username
        /// </summary>
        public string TrcUsername { get; set; }

        /// <summary>
        /// response status code
        /// </summary>
        public int TrcStatusCode { get; set; }

        /// <summary>
        /// start time of request
        /// </summary>
        public DateTime TrcStartTime { get; set; }

        /// <summary>
        /// duration of request
        /// </summary>
        public float TrcDuration { get; set; }


        /// <summary>
        /// name of the server
        /// </summary>
        public string TrcMachineName { get; set; }

        /// <summary>
        /// url path incase of error
        /// </summary>
        public string TrcErrorPath { get; set; }

        /// <summary>
        /// request type
        /// </summary>
        public string TrcRequestType { get; set; }

        /// <summary>
        /// request verb
        /// </summary>
        public string TrcRequestVerb { get; set; }

        /// <summary>
        /// request correlation id
        /// </summary>
        public string TrcCorrelationID { get; set; }

        /// <summary>
        /// request json rpc error code
        /// </summary>
        public int? TrcJsonRpcErrorCode { get; set; }


        /// <summary>
        /// request referrer
        /// </summary>
        public string TrcReferrer { get; set; }


        /// <summary>
        /// request content length
        /// </summary>
        public long? TrcContentLength { get; set; }
        
        #endregion


    }
}