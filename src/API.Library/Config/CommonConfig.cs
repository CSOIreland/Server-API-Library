﻿using Microsoft.Extensions.Options;

namespace API
{
    public static class CommonConfig 
    {

        public static bool distributedConfigCheck(decimal? appSettingsVersion, decimal? inMemoryVersion, string configType,string inputDTO, IDictionary<string, string> apiDict, IDictionary<string, string> appDict)
        {
            if (appSettingsVersion != null )
            {
                return false;
            }
            else if (appSettingsVersion == null && ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == false)
            {
                Log.Instance.Fatal("Memcache must be enabled if version is null");
                return false;
            }
            else { 
                if (!ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED)
                {
                    Log.Instance.Error("Configuration Error: Memcache is disabled but version is null.");
                    return false;
                }
                MemCachedD_Value version_data = ApiServicesHelper.CacheD.Get_BSO<dynamic>(configType, "Configuration", "Version", inputDTO);

                //if record exists in cache
                if (version_data.hasData)
                {
                    decimal cacheVersion;
                    if (!decimal.TryParse(version_data.data.Value.ToString(), out cacheVersion) ){
                        Log.Instance.Fatal("Unable to parse "+ configType + " config version");
                        return false;
                    }

                    IDictionary<string, string> dictN;

                    if (configType.Equals("APP"))
                    {
                        dictN = appDict;
                    }
                    else if (configType.Equals("API"))
                    {
                        dictN = apiDict;
                    }
                    else
                    {
                        Log.Instance.Fatal("Unknown configuration type : " + configType);
                        throw new Exception("Unknown configuration type : " + configType);
                    }

                    if (cacheVersion == inMemoryVersion && dictN != null)
                    {
                        //dictionary is already populated with a version so dont need to go to the database
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }


        public static IDictionary<string, string> ReadJSONSettings(decimal? inMemoryVersion, string ConfigType)
        {
            if (inMemoryVersion == null)
            {
                Log.Instance.Fatal(""+ ConfigType + " : When using a JSON file the version must be specified");
                throw new Exception("" + ConfigType + "  : When using a JSON file the version must be specified");
            }
            else
            {
                var dictionary = new Dictionary<string, string>();
                string[] configElements = { ""+ConfigType+"" };
                for (var i = 0; i < configElements.Length; i++)
                {
                    var data = ApiServicesHelper.Configuration.GetSection(configElements[i]).GetChildren();
                    foreach (var val in data)
                    {
                        if (val.Key == "CONFIG_VERSION")
                        {
                            if (inMemoryVersion != Convert.ToDecimal(val.Value))
                            {
                                Log.Instance.Fatal("" + ConfigType + " : Configuration version not found for version " + inMemoryVersion);
                            }
                        }
                        if (dictionary.ContainsKey(val.Key))
                        {
                            Log.Instance.Fatal("Duplicate "+ ConfigType + " Config Key detected : " + val.Key);
                        }
                        else
                        {
                            dictionary.Add(val.Key, val.Value);
                        }
                    }
                }
                Log.Instance.Info("Json "+ ConfigType + " configuration loaded");

                return dictionary;
            }
        }
  

        public static void deployUpdate(IADO ado, decimal? inMemoryVersion, string configType)
        {
            var inputParamList = new List<ADO_inputParams>()
                {
                    new ADO_inputParams() {name= "@app_settings_version", value = inMemoryVersion},
                    new ADO_inputParams() {name= "@config_setting_type", value = configType}
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
                Log.Instance.Fatal("failed to insert into App_Setting_Deploy_Update : version - " + inMemoryVersion + " , config_setting_type - "+ configType + "");
                Log.Instance.Fatal(ex.ToString());
            }
            finally
            {
                //now close the connection
                ado.CloseConnection(true);
            }
        }

        public static void memcacheSave(decimal? appSettingsVersion, decimal? inMemoryVersion, string configType,string inputDTO, IDictionary<string, string> dict)
        {
              if (appSettingsVersion == null && ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == true)
                {
                    ApiServicesHelper.CacheD.Store_BSO<dynamic>(configType, "Configuration", "Version", inputDTO, inMemoryVersion, DateTime.Today.AddDays(30));
                }
            else if (appSettingsVersion == null && ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED == false)
                {
                Log.Instance.Fatal("Unable to store configuration version in memecache as it is disabled");
                }
        }
    }
}
