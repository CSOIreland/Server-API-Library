using Microsoft.Extensions.Options;


namespace API
{
    public class ApiConfiguration : IApiConfiguration
    {
        internal static IOptionsMonitor<APIConfig> _ApplicationSettingsDelegate;

        /// <summary>
        /// Initiate APPSettings 
        /// </summary>
        static IDictionary<string, string> apiCongfiguration;

        static decimal version = 0;
        public ApiConfiguration(IOptionsMonitor<APIConfig> ApplicationSettingsDelegate)
        {
            _ApplicationSettingsDelegate = ApplicationSettingsDelegate;

            Log.Instance.Info("load api settings");
            if (apiCongfiguration == null)
            {
                    version = _ApplicationSettingsDelegate.CurrentValue.version;
                    ReadAppSettings();
            }
            else
            {
                apiCongfiguration = CheckSettingsAreCurrent(apiCongfiguration);
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
                Log.Instance.Info("get api settings");
                return CheckSettingsAreCurrent(apiCongfiguration);
            }
        }

        /// <summary>
        /// Checks using the version in the appsettings.json file if there has been a change.
        /// if there has been a change then using that version number get the data from the database
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, string> CheckSettingsAreCurrent(IDictionary<string, string> appSettings)
        {
            decimal TempVersion = _ApplicationSettingsDelegate.CurrentValue.version;
            if (TempVersion != version || appSettings == null)
            {
                version = _ApplicationSettingsDelegate.CurrentValue.version;
                ReadAppSettings();
            }
            return appSettings;
        }

        private void ReadAppSettings() 
        {
            if (ApiServicesHelper.APIConfig.Settings_Type == "JSONFile")
            {
                apiCongfiguration = ReadJSONAppSettings();
            }
            else if (ApiServicesHelper.APIConfig.Settings_Type == "DB")
            {
                apiCongfiguration = ReadDBAppSettings(new ADO());
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
                new ADO_inputParams() { name = "@app_settings_version", value = version },
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


            foreach (var c in output.data)
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

            var inputParamList = new List<ADO_inputParams>()
            {
                new ADO_inputParams() {name= "@app_settings_version", value = version},
                new ADO_inputParams() {name= "@config_setting_type", value = "API"},
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
                Log.Instance.Fatal("failed to insert into App_Setting_Deploy_Update : version - " + version + " , config_setting_type - API");
                Log.Instance.Fatal(ex.ToString());
            }
            finally{
                //now close the connection
                ado.CloseConnection(true);
            }

            return dictionary;
        }

        private static IDictionary<string, string> ReadJSONAppSettings()
        {
           var dictionary = new Dictionary<string, string>();
            string[] configElements = { "apiConfig" };
            for (var i =0; i < configElements.Length; i++)
            {
                var data = ApiServicesHelper.Configuration.GetSection(configElements[i]).GetChildren();
                foreach (var val in data)
                {
                    if(val.Key == "CONFIG_VERSION")
                    {
                        if(version != Convert.ToDecimal(val.Value))
                        {
                            Log.Instance.Fatal("API Configration version not found for version " + version);
                        }
                    }

                    if (dictionary.ContainsKey(val.Key)){
                        Log.Instance.Fatal("Duplicate API Config Key detected : " + val.Key);
                    }
                    else
                    {
                        dictionary.Add(val.Key, val.Value);
                    }
                   
                }
            }
            Log.Instance.Info("Json api configuration loaded");
            return dictionary;
        }

    }
}
