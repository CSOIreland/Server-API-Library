using API;
using Newtonsoft.Json;

namespace Sample
{
    [AllowAPICall]

    /// <summary>
    /// 
    /// </summary>
    public class YourMemCacheD
    {
        #region Methods
        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a JSONRPC_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic YourExposedMethod(JSONRPC_API apiRequest)
        {
            // Initiate new object to handle the API Output
            JSONRPC_Output output = new JSONRPC_Output();
            ICacheD _iCacheD = AppServicesHelper.ServiceProvider.GetService<ICacheD>();

            try
            {
                // Set the input DTO 
                YourDTO inputDTO = new YourDTO();

                // Set the Data to be cached
                dynamic data = "Test";

                // Set the validity time of the cache (ie. 3600 seconds). If no TimeSpan is provided then the MAX validity is taken (ie. 30 dyas)
                TimeSpan validFor = new TimeSpan(3600 * TimeSpan.TicksPerSecond);
                // Store the data in the cache as you need
                bool successStore = _iCacheD.Store_BSO<YourDTO>("Sample", "MemCacheD", "YourExposedMethod", inputDTO, data, validFor);

                // Yuo can store as many cached entries into a Repository as well
                /*
                 * bool successStore = MemCacheD.Store_BSO<YourDTO>("Sample", "MemCacheD", "YourExposedMethod", inputDTO, data, validFor, "MyRepoSample");
                 */

                // Otherwise, set the expiry date of the cache (ie. tomorrow at midnight)
                // Store the data in the cache as you need
                /*
                 * DateTime expiresAt = DateTime.Today.AddDays(1);
                 * bool successStore = MemCacheD.Store_BSO<YourDTO>("Sample", "MemCacheD", "YourExposedMethod", inputDTO, data, expiresAt);
                 */
                if (successStore)
                {
                    // Get the data from the cache as you need
                    // Similar approach for MemCacheD.Get_ADO
                    MemCachedD_Value valueCached = _iCacheD.Get_BSO<YourDTO>("Sample", "MemCacheD", "YourExposedMethod", inputDTO);

                    output.data = valueCached;
                    Log.Instance.Debug("Value from cache: " + JsonConvert.SerializeObject(valueCached));
                }

                // Remove an entry from the cache as you need
                // Similar approach for MemCacheD.Remove_ADO
                bool successRemove = _iCacheD.Remove_BSO<YourDTO>("Sample", "MemCacheD", "YourExposedMethod", inputDTO);

                // Yuo can flush a full Repository as you need
                //MemCacheD.CasRepositoryFlush("MyRepoSample");

                // Flush all the cached records as you need... but be careful!
                _iCacheD.FlushAll();
            }
            catch (Exception e)
            {
                // If an error/exception occurs, then return the error to the API
                output.error = e;
                // Log an error message for your sake :-)
                Log.Instance.Error(e);
            }

            return output;
        }

        /// <summary>
        /// This method is internal and not exposed to the API but accessible within your Assemply project.
        /// </summary>
        internal static void YourInternalMethod()
        {
            // All your internal business logic goes here
        }

        /// <summary>
        /// This method is private and not exposed to the API and accessible only within your Class.
        /// </summary>
        private static void YourPrivateMethods()
        {
            // All your private business logic goes here
        }
        #endregion
    }

    /// <summary>
    /// Sampe DTO for testing
    /// </summary>
    public class YourDTO
    {
        #region Properties
        /// <summary>
        /// 
        /// </summary>
        string param_1;

        /// <summary>
        /// 
        /// </summary>
        string param_2;

        /// <summary>
        /// 
        /// </summary>
        string param_3;

        //...

        /// <summary>
        /// 
        /// </summary>
        string param_N;

        #endregion
    }
}
