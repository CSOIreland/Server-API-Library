using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;

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
                                return null;
                            }
                        }
                        if (dictionary.ContainsKey(val.Key))
                        {
                            Log.Instance.Fatal("Duplicate "+ ConfigType + " Config Key detected : " + val.Key);
                        }
                        else
                        {
                         
                            var combinedValue = CombineSectionValues(val);
                            dictionary.Add(val.Key, combinedValue);
                        }
                    }
                }
                Log.Instance.Info("Json "+ ConfigType + " configuration loaded");

                return dictionary;
            }
        }


       // Method to recursively combine section values into a JSON string
       public static string CombineSectionValues(IConfigurationSection section)
        {
            // Get the children of the current section
            var children = section.GetChildren();

            // If there are no children, return the section value
            if (!children.Any())
            {
                return section.Value;
            }

            // Create a JSON object to hold the combined values
            var jsonObject = new JObject();

            // Recursively process each child section
            foreach (var child in children)
            {
                jsonObject[child.Key] = CombineSectionValues(child);
            }

            // Convert the JSON object to a string and return it
            return jsonObject.ToString();
        }

        public static void deployUpdate( decimal? inMemoryVersion, string configType)
        {          
            var inputParamList = new List<ADO_inputParams>()
            {
               new ADO_inputParams() {name= "@app_settings_version", value = inMemoryVersion},
               new ADO_inputParams() {name= "@config_setting_type", value = configType}
            };

            var retParam = new ADO_returnParam();
            retParam.name = "return";
            retParam.value = 0;
            IADO ado = new ADO(); 
            try
            {
                ado.ExecuteNonQueryProcedure("App_Setting_Deploy_Update", inputParamList, ref retParam);
            }
            catch (Exception ex)
            {
                //log the audit insert failed but no need to raise error.
                Log.Instance.Fatal("failed to insert into App_Setting_Deploy_Update : version - " + inMemoryVersion + " , config_setting_type - "+ configType + "");
                Log.Instance.Fatal(ex.ToString());
            }
            finally
            {
                //now close the connection
                ado.Dispose();
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
