
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

            if (distributed_config == false && version == null)
            {
                Log.Instance.Fatal("APP : Distributed config must be true if version is null");
                throw new Exception("APP : Distributed config must be true if version is null");
            }
            else if (distributed_config == true && version != null)
            {
                Log.Instance.Fatal("APP : Distributed config must be false if version is not null");
                throw new Exception("APP : Distributed config must be false if version is not null");
            }
            else
            {
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
        /// Gets the distribtued flag from
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public bool distributed_config
        {
            get
            {
                return _ApplicationSettingsDelegate.CurrentValue.distributed_config;
            }
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

            if (CommonConfig.distributedConfigCheck(version, inMemoryVersion, distributed_config, "APP", "app_config_version", ApiServicesHelper.ApiConfiguration.Settings, appSettings))
            {
                //we have valid config
                if (appSettings == null)
                {
                    ReadAppSettings();
                }
            }
            else
            {

                if (distributed_config == false && version == null)
                {
                    Log.Instance.Fatal("APP : Distributed config must be true if version is null");
                    throw new Exception("APP : Distributed config must be true if version is null");
                }
                else if (distributed_config == true && version != null)
                {
                    Log.Instance.Fatal("APP : Distributed config must be false if version is not null");
                    throw new Exception("APP : Distributed config must be false if version is not null");
                }
                else
                {
                    if (version != inMemoryVersion || appSettings == null)
                    {
                        inMemoryVersion = version;
                        ReadAppSettings();
                    }
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

                if (distributed_config == true)
                {
                    //update memcache
                    CommonConfig.memcacheSave(inMemoryVersion, "APP", "app_config_version", distributed_config, appConfiguration);
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
            try
            {
                // No transaction required
                output = ado.ExecuteReaderProcedure("App_Settings_Read", paramList);

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
                if (dictionary.ContainsKey(c.APP_KEY))
                {
                    Log.Instance.Fatal("Duplicate APP Config Key detected : " + c.APS_KEY);
                }
                else
                {
                    dictionary.Add(c.APP_KEY, c.APP_VALUE);
                }
            }

            var tVersion = output.data[1];
            inMemoryVersion = tVersion[0].max_version_number;

            CommonConfig.deployUpdate(ado, inMemoryVersion, "APP");

            return dictionary;
        }
    }
}
