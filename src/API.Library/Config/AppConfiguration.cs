using MessagePack;
using Microsoft.Extensions.Options;

namespace API
{
    public class APPConfiguration : IAppConfiguration
    {
        internal static IOptionsMonitor<APPConfig> _ApplicationSettingsDelegate;

        /// <summary>
        /// Initiate APPSettings 
        /// </summary>
        static IDictionary<string, string> appCongfiguration;

        static decimal version = 0;

        public APPConfiguration(IOptionsMonitor<APPConfig> ApplicationSettingsDelegate)
        {
            if (ApiServicesHelper.APPConfig.enabled && ApiServicesHelper.ApplicationLoaded)
            {
                Log.Instance.Info("Load app settings");
                _ApplicationSettingsDelegate = ApplicationSettingsDelegate;
                distributedConfigCheck();

                if (appCongfiguration == null)
                {
                  ReadAppSettings();
                }
                else
                {
                    appCongfiguration = CheckSettingsAreCurrent(appCongfiguration);
                }
            }
        }

        /// <summary>
        /// Gets the auto_version flag from
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public bool auto_version
        {
            get
            {
                Log.Instance.Info("get app settings");
                return _ApplicationSettingsDelegate.CurrentValue.auto_version;
            }
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
        /// Gets the latest APP settings from the database using the key from the 
        /// appsettings.json 
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> Settings
        {
            get
            {
                return CheckSettingsAreCurrent(appCongfiguration);
            }
        }

        /// <summary>
        /// Checks using the version in the appsettings.json file if there has been a change.
        /// if there has been a change then using that version number get the data from the database
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, string> CheckSettingsAreCurrent(IDictionary<string, string> appSettings)
        {
            if (ApiServicesHelper.APPConfig.enabled && ApiServicesHelper.ApplicationLoaded)
            {
                if(!distributedConfigCheck())
                {
                    decimal TempVersion = _ApplicationSettingsDelegate.CurrentValue.version;
                    if (TempVersion != version || appSettings == null || auto_version == true)
                    {
                        version = TempVersion;
                        ReadAppSettings();
                    }
                }
                else
                {
                    if(appSettings == null)
                    {
                        ReadAppSettings();
                    }
                }
            }
            return appSettings;
        }

        private void ReadAppSettings() 
        {
            version = _ApplicationSettingsDelegate.CurrentValue.version;

            if (ApiServicesHelper.APPConfig.Settings_Type == "JSONFile")
            {
                appCongfiguration = ReadJSONAppSettings();
            }
            else if (ApiServicesHelper.APPConfig.Settings_Type == "DB")
            {
                appCongfiguration = ReadDBAppSettings(new ADO());
            }
            else
            {
                Log.Instance.Fatal("Unsupported configuration location for APP Config");
            }
        }

        private bool distributedConfigCheck()
        {
            if (distributed_config == false)
            {
                return false;
            }
            else if (distributed_config == true && auto_version == true)
            {
                if (!Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_MEMCACHED_ENABLED"]))
                {
                    Log.Instance.Error("Configuration Error: Memcache is disabled but distributed flag is true.");
                    ApiServicesHelper.ApplicationLoaded = false;
                    return false;
                }

                MemCachedD_Value version_data = ApiServicesHelper.CacheD.Get_BSO<dynamic>("App", "Configuration", "Version", "app_config_version");

                //if record exists in cache
                if (version_data.hasData)
                {
                    decimal cacheVersion;
                    if (!decimal.TryParse(version_data.data.Value.ToString(), out cacheVersion) ){
                        Log.Instance.Fatal("Unable to parse app config verison");
                        return false;
                    }

                    if (cacheVersion == version && appCongfiguration != null)
                    {
                        //dictionary is already populated with a version so dont need to go to the database
                        return true;
                    }
                    else if (cacheVersion == version && appCongfiguration == null)
                    {
                        version = (decimal)version_data.data.Value;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            } else if (distributed_config == true && auto_version == false)
            {
                Log.Instance.Fatal("Distributed config and auto version must be both true");
                return false;
            }
            return false;
        }
        private IDictionary<string, string> ReadDBAppSettings(IADO ado)
        {

            var output = new ADO_readerOutput();
            var paramList = new List<ADO_inputParams>
            {
                new ADO_inputParams() { name = "@app_settings_version", value = version },
                new ADO_inputParams() { name = "@auto_version", value = auto_version }
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

            var inputParamList = new List<ADO_inputParams>()
            {
                new ADO_inputParams() {name= "@app_settings_version", value = version},
                new ADO_inputParams() {name= "@config_setting_type", value = "APP"},
                new ADO_inputParams() { name = "@auto_version", value = auto_version }
            };

            var retParam = new ADO_returnParam();
            retParam.name = "return";
            retParam.value = 0;

            try
            {
                ado.StartTransaction();
                ado.ExecuteNonQueryProcedure("App_Setting_Deploy_Update", inputParamList, ref retParam);
                ado.CommitTransaction();
            }
            catch (Exception ex)
            {
                ado.RollbackTransaction();
                //log the audit insert failed but no need to raise error.
                Log.Instance.Fatal("failed to insert into App_Setting_Deploy_Update : version - " + version + " , config_setting_type - APP," + " auto_version  " + auto_version); 
                Log.Instance.Fatal(ex.ToString());
            }
            finally
            {
                //now close the connection
                ado.CloseConnection(true);
            }

            var  tVersion = output.data[1];

            if(distributed_config == true)
            {
                //update memcache
                ApiServicesHelper.CacheD.Store_BSO<dynamic>("App", "Configuration", "Version", "app_config_version", tVersion[0].max_version_number, DateTime.Today.AddDays(30));
                version = tVersion[0].max_version_number;
            }

            return dictionary;
        }

        private static IDictionary<string, string> ReadJSONAppSettings()
        {
           var dictionary = new Dictionary<string, string>();
            string[] configElements = { "appConfig"};
            for (var i =0; i < configElements.Length; i++)
            {
                var data = ApiServicesHelper.Configuration.GetSection(configElements[i]).GetChildren();
                foreach (var val in data)
                {
                    if(val.Key == "CONFIG_VERSION")
                    {
                        if(version != Convert.ToDecimal(val.Value))
                        {
                            Log.Instance.Fatal("APP Configration version not found for version " + version);
                        }
                    }
                    if (dictionary.ContainsKey(val.Key))
                    {
                        Log.Instance.Fatal("Duplicate APP Config Key detected : " + val.Key);
                    }
                    else
                    {
                        dictionary.Add(val.Key, val.Value);
                    }
                }
            }
            Log.Instance.Info("Json app configuration loaded");

            return dictionary;
        }

    }
}
