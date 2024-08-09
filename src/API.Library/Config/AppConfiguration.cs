
using Microsoft.Extensions.Options;

namespace API
{
    public class APPConfiguration : IAppConfiguration
    {
        internal static IOptionsMonitor<APPConfig> _ApplicationSettingsDelegate;

        /// <summary>
        /// Initiate APPSettings 
        /// </summary>
        static IDictionary<string, string> appConfiguration;

        static decimal? inMemoryVersion = null;
        public APPConfiguration(IOptionsMonitor<APPConfig> ApplicationSettingsDelegate)
        {

            _ApplicationSettingsDelegate = ApplicationSettingsDelegate;

            if (ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == false && version == null)
            {
                Log.Instance.Fatal("APP : Memcache should be enabled if version is null");
            }

                Log.Instance.Info("load APP settings");
                if (appConfiguration == null)
                {
                    inMemoryVersion = version;
                    ReadAppSettings();
                }
                else
                {
                    appConfiguration = CheckSettingsAreCurrent(appConfiguration);
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
                return appConfiguration;
            }
        }

        public void Refresh()
        {
            Log.Instance.Info("refresh app settings");
            //refresh appConfiguration if necessary
            CheckSettingsAreCurrent(appConfiguration);
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
        /// Checks using the version in the appsettings.json file if there has been a change.
        /// if there has been a change then using that version number get the data from the database
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, string> CheckSettingsAreCurrent(IDictionary<string, string> appSettings)
        {

            if (CommonConfig.distributedConfigCheck(version, inMemoryVersion, "APP", "app_config_version", ApiServicesHelper.ApiConfiguration.Settings, appSettings))
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
                    Log.Instance.Fatal("APP : Memcache should be enabled if version is null");
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
            if (ApiServicesHelper.APPConfig.Settings_Type == "JSONFile")
            {
                appConfiguration = CommonConfig.ReadJSONSettings(inMemoryVersion, "appConfig");
            }
            else if (ApiServicesHelper.APPConfig.Settings_Type == "DB")
            {
                appConfiguration = ReadDBAppSettings(new ADO());

                if (version == null)
                {
                    //update memcache
                    CommonConfig.memcacheSave(version,inMemoryVersion, "APP", "app_config_version", appConfiguration);
                }
            }
            else
            {
                Log.Instance.Fatal("Unsupported APP configuration location");
            }
        }


        private IDictionary<string, string> ReadDBAppSettings(IADO ado)
        {
            var output = new ADO_readerOutput();
            var paramList = new List<ADO_inputParams>
            {
                new ADO_inputParams() { name = "@app_settings_version", value = inMemoryVersion ?? (object)System.DBNull.Value },
            };

            var dictionary = new Dictionary<string, string>();


            try
            {
                // No transaction required
                output = ado.ExecuteReaderProcedure("App_Settings_Read", paramList);
                    
                foreach (var c in output.data[0])
                {
                    if (dictionary.ContainsKey(c.APP_KEY))
                    {
                        Log.Instance.Fatal("Duplicate APP Config Key detected : " + c.APP_KEY);
                    }
                    else
                    {
                        dictionary.Add(c.APP_KEY, c.APP_VALUE);
                    }
                }

                var tVersion = output.data[1];
                inMemoryVersion = tVersion[0].max_version_number;

                CommonConfig.deployUpdate( inMemoryVersion, "APP");

            }
            catch (Exception ex)
            {
                ado.Dispose();
                Log.Instance.Fatal(ex.ToString());
                //version number not found in database
                throw ex;
            }


            return dictionary;
        }
    }
}
