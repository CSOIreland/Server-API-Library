using Microsoft.Extensions.Options;

namespace API
{
    public class ApiConfiguration : IApiConfiguration
    {
        internal static IOptionsMonitor<APIConfig> _ApplicationSettingsDelegate;

        /// <summary>
        /// Initiate APPSettings 
        /// </summary>
        static IDictionary<string, string> apiConfiguration;

        static decimal? inMemoryVersion = null;
        public ApiConfiguration(IOptionsMonitor<APIConfig> ApplicationSettingsDelegate)
        {

            _ApplicationSettingsDelegate = ApplicationSettingsDelegate;

            if (version == null && ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == false)
            {
                Log.Instance.Fatal("API : Memcache should be enabled if version is null");
            }
           
                Log.Instance.Info("load api settings");
                if (apiConfiguration == null)
                {
                    inMemoryVersion = version;
                    ReadAppSettings();
                }
                else
                {
                    apiConfiguration = CheckSettingsAreCurrent(apiConfiguration);
                }
            
        }

        /// <summary>
        /// Gets the maintenance flag from the app settings json file 
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public bool MAINTENANCE
        {
            get
            {
                return _ApplicationSettingsDelegate.CurrentValue.API_MAINTENANCE;
            }
        }


        /// <summary>
        /// Gets the latest APP settings from the database using the key from the 
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> Settings
        {
            get
            {
                return apiConfiguration;
            }
        }

        public void Refresh()
        {
            Log.Instance.Info("refresh app settings");
            //refresh apiConfiguration if necessary
            CheckSettingsAreCurrent(apiConfiguration);
        }

        /// <summary>
        /// Gets the version from
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public decimal? version
        {
            get
            {
                return _ApplicationSettingsDelegate.CurrentValue.version;
            }
        }

        /// <summary>
        /// Gets the API_TRACE_RECORD_IP from
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public bool API_TRACE_RECORD_IP
        {
            get
            {
                return _ApplicationSettingsDelegate.CurrentValue.API_TRACE_RECORD_IP;
            }
        }

        /// <summary>
        /// Gets the API_TRACE_ENABLED from
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public bool API_TRACE_ENABLED
        {
            get
            {
                return _ApplicationSettingsDelegate.CurrentValue.API_TRACE_ENABLED;
            }
        }


        /// <summary>
        /// Checks using the version in the appsettings.json file if there has been a change.
        /// if there has been a change then using that version number get the data from the database
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, string> CheckSettingsAreCurrent(IDictionary<string, string> appSettings)
        {

            if (CommonConfig.distributedConfigCheck(version, inMemoryVersion, "API", "api_config_version", appSettings,null))
            {
                //we have valid config
                if (appSettings == null)
                {
                    ReadAppSettings();
                }
            }
            else
            {

                if (ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == false && version == null)
                {
                    Log.Instance.Fatal("API : Memcache should be enabled if version is null");
                }
            
                    if (version != inMemoryVersion || appSettings == null)
                    {
                        inMemoryVersion = version;
                       ReadAppSettings();
                    }
               
            }
            return appSettings;
        }

        private void ReadAppSettings() 
        {
            if (ApiServicesHelper.APIConfig.Settings_Type == "JSONFile")
            {
                apiConfiguration = CommonConfig.ReadJSONSettings(inMemoryVersion, "apiConfig");
            }
            else if (ApiServicesHelper.APIConfig.Settings_Type == "DB")
            {
                apiConfiguration = ReadDBAppSettings(new ADO());
                
                if (version == null)
                {
                    //update memcache
                    CommonConfig.memcacheSave(version, inMemoryVersion, "API", "api_config_version", apiConfiguration);
                }
            }
            else
            {
                Log.Instance.Fatal("Unsupported API configuration location");
            }
        }


        private IDictionary<string, string> ReadDBAppSettings(IADO ado)
        {
            var output = new ADO_readerOutput();
            var paramList = new List<ADO_inputParams>
            {
                new ADO_inputParams() { name = "@app_settings_version", value = inMemoryVersion ?? (object)System.DBNull.Value },
            };
            try
            {
                // No transaction required
                output = ado.ExecuteReaderProcedure("Api_Settings_Read", paramList);

            }
            catch (Exception ex)
            {
                ado.CloseConnection();
                Log.Instance.Fatal(ex.ToString());
                //version number not found in database
                throw ex;
            }

            var dictionary = new Dictionary<string, string>();

            foreach (var c in output.data[0])
            {
                if (dictionary.ContainsKey(c.API_KEY))
                {
                    Log.Instance.Fatal("Duplicate API Config Key detected : " + c.API_KEY);
                }
                else
                {
                    dictionary.Add(c.API_KEY, c.API_VALUE);
                }               
            }

            var tVersion = output.data[1];
            inMemoryVersion = tVersion[0].max_version_number;

            CommonConfig.deployUpdate(ado, inMemoryVersion, "API");

            return dictionary;
        }
    }
}
