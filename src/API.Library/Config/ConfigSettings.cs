namespace API
{
    public class ADOSettings
    {
        public string API_ADO_DEFAULT_CONNECTION { get; set; }
        public string API_PERFORMANCE_DATABASE { get; set; }
        public string API_TRACE_DATABASE { get; set; }
        public int API_ADO_EXECUTION_TIMEOUT { get; set; }
        public int API_ADO_BULKCOPY_TIMEOUT { get; internal set; }
        public int API_ADO_BULKCOPY_BATCHSIZE { get; set; }
    }

    public class APIConfig
    {
        public decimal version { get; set; }
        public bool API_MAINTENANCE { get; set; }
        public string Settings_Type { get; set; }
    }

    public class APPConfig
    {
        public bool enabled { get; set; }
        public decimal version { get; set; }
       
        public string Settings_Type { get; set; }

        public bool auto_version { get; set; }

        public bool distributed_config { get; set; }
        
    }



    public class APISettings
    {
        public Settings jsonrpc { get; set; }
        public Settings restful { get; set; }
        public Settings Static { get; set; }
        public Settings head { get; set; }
    }

    public class Settings
    {
        public bool allowed { get; set; }
        public List<string> verb { get; set; }
    }

    public class BlockedRequests
    {
        public List<string> urls { get; set; }
    }

}
