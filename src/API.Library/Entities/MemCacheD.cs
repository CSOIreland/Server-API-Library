﻿using System;
using System.Configuration;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace API
{
    /// <summary>
    /// Handling MemCacheD based on the Enyim client
    /// </summary>
    public static class MemCacheD
    {
        #region Properties
        /// <summary>
        /// Flag to indicate if MemCacheD is enabled 
        /// </summary>
        internal static bool API_MEMCACHED_ENABLED = Convert.ToBoolean(ConfigurationManager.AppSettings["API_MEMCACHED_ENABLED"]);

        /// <summary>
        /// Maximum validity in number of seconds that MemCacheD can handle (30 days = 2592000)
        /// </summary>
        internal static uint API_MEMCACHED_MAX_VALIDITY = Convert.ToUInt32(ConfigurationManager.AppSettings["API_MEMCACHED_MAX_VALIDITY"]);

        /// <summary>
        /// Salsa code to isolate the cache records form other applications or environments
        /// </summary>
        internal static string API_MEMCACHED_SALSA = ConfigurationManager.AppSettings["API_MEMCACHED_SALSA"];

        /// <summary>
        /// Max TimeSpan
        /// </summary>
        internal static TimeSpan maxTimeSpan = new TimeSpan(API_MEMCACHED_MAX_VALIDITY * TimeSpan.TicksPerSecond);

        /// <summary>
        /// Initiate MemCacheD
        /// </summary>
        internal static MemcachedClient MemcachedClient = API_MEMCACHED_ENABLED ? new MemcachedClient() : null;

        #endregion

        #region Methods
        /// <summary>
        /// Check if MemCacheD is enabled
        /// </summary>
        /// <returns></returns>
        private static bool IsEnabled()
        {
            Log.Instance.Info("MemCacheD Enabled: " + API_MEMCACHED_ENABLED);

            if (API_MEMCACHED_ENABLED)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Generate the Key for ADO
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        private static string GenerateKey_ADO(string nameSpace, string procedureName, List<ADO_inputParams> inputParams)
        {
            // Initiate
            string hashKey = "";

            try
            {
                // Initilise a new dynamic object
                dynamic keyObject = new ExpandoObject();
                // Implement the interface for handling dynamic properties
                var keyObject_IDictionary = keyObject as IDictionary<string, object>;

                // Add the reference to the nameSpace to the Object
                keyObject_IDictionary.Add("nameSpace", nameSpace);

                // Add the reference to the Procedure to the Object
                keyObject_IDictionary.Add("procedureName", procedureName);

                // Add the reference to the Params to the Object
                keyObject_IDictionary.Add("inputParams", Utility.JsonSerialize_IgnoreLoopingReference(inputParams));

                // Generate the composed Key
                hashKey = GenerateHash(keyObject_IDictionary);
                return hashKey;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return hashKey;
            }
        }

        /// <summary>
        /// Generate the Key for ADO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <returns></returns>
        private static string GenerateKey_BSO<T>(string nameSpace, string className, string methodName, T inputDTO)
        {
            // Initiate
            string hashKey = "";

            try
            {
                // Initilise a new dynamic object
                dynamic keyObject = new ExpandoObject();
                // Implement the interface for handling dynamic properties
                var keyObject_IDictionary = keyObject as IDictionary<string, object>;

                // Add the reference to the nameSpace to the Object
                keyObject_IDictionary.Add("nameSpace", nameSpace);

                // Add the reference to the className to the Object
                keyObject_IDictionary.Add("className", className);

                // Add the reference to the methodName to the Object
                keyObject_IDictionary.Add("methodName", methodName);

                // Add the reference to the inputDTO to the Object
                keyObject_IDictionary.Add("inputDTO", Utility.JsonSerialize_IgnoreLoopingReference(inputDTO));

                // Generate the composed Key
                hashKey = GenerateHash(keyObject_IDictionary);
                return hashKey;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return hashKey;
            }
        }

        /// <summary>
        /// Generate the Hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string GenerateHash(IDictionary<string, object> input)
        {
            // Initiate
            string hashKey = "";

            try
            {
                // Append the SALSA code
                input.Add("salsa", API_MEMCACHED_SALSA);

                hashKey = Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(input));
                return hashKey;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return hashKey;
            }
        }

        /// <summary>
        /// Store a record for ADO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <param name="data"></param>
        /// <param name="expiresAt"></param>
        /// <param name="validFor"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        private static bool Store_ADO<T>(string nameSpace, string procedureName, List<ADO_inputParams> inputParams, dynamic data, DateTime expiresAt, TimeSpan validFor, string repository)
        {
            // Check if it's enabled first
            if (!IsEnabled())
            {
                return false;
            }

            // Validate Expiry parameters
            if (!ValidateExpiry(ref expiresAt, ref validFor))
            {
                return false;
            }

            // Get the Key
            string key = GenerateKey_ADO(nameSpace, procedureName, inputParams);

            // Get the Value
            MemCachedD_Value value = SetValue(data, expiresAt, validFor);

            // Store the Value by Key
            if (Store(key, value, validFor, repository))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Store a record for ADO with an expiry date
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <param name="data"></param>
        /// <param name="expiresAt"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static bool Store_ADO<T>(string nameSpace, string procedureName, List<ADO_inputParams> inputParams, dynamic data, DateTime expiresAt, string repository = null)
        {
            return Store_ADO<T>(nameSpace, procedureName, inputParams, data, expiresAt, new TimeSpan(0), repository);
        }

        /// <summary>
        /// Store a record for ADO with a validity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <param name="data"></param>
        /// <param name="validFor"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static bool Store_ADO<T>(string nameSpace, string procedureName, List<ADO_inputParams> inputParams, dynamic data, TimeSpan validFor, string repository = null)
        {
            return Store_ADO<T>(nameSpace, procedureName, inputParams, data, new DateTime(0), validFor, repository);
        }

        /// <summary>
        /// Store a record for BSO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <param name="data"></param>
        /// <param name="expiresAt"></param>
        /// <param name="validFor"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        private static bool Store_BSO<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, DateTime expiresAt, TimeSpan validFor, string repository)
        {
            // Check if it's enabled first
            if (!IsEnabled())
            {
                return false;
            }

            // Validate Expiry parameters
            if (!ValidateExpiry(ref expiresAt, ref validFor))
            {
                return false;
            }

            // Get the Key
            string key = GenerateKey_BSO(nameSpace, className, methodName, inputDTO);

            // Get the Value
            MemCachedD_Value value = SetValue(data, expiresAt, validFor);

            // Store the Value by Key
            if (Store(key, value, validFor, repository))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Store a record for BSO with a expiry date
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <param name="data"></param>
        /// <param name="expiresAt"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static bool Store_BSO<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, DateTime expiresAt, string repository = null)
        {
            return Store_BSO<T>(nameSpace, className, methodName, inputDTO, data, expiresAt, new TimeSpan(0), repository);
        }

        /// <summary>
        /// Store a record for BSO with a validity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <param name="data"></param>
        /// <param name="validFor"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static bool Store_BSO<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, TimeSpan validFor, string repository = null)
        {
            return Store_BSO<T>(nameSpace, className, methodName, inputDTO, data, new DateTime(0), validFor, repository);
        }

        /// <summary>
        /// Store the value object by key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="validFor"></param>
        /// <returns></returns>
        private static bool Store(string key, MemCachedD_Value value, TimeSpan validFor, string repository)
        {
            try
            {
                // The value must be serialised
                string serializedValue = JsonConvert.SerializeObject(value);

                bool isStored = false;

                // Fix the MemCacheD issue with bad Windows' compiled version: use validFor instead of expiresAt
                // validFor and expiresAt match each other
                isStored = MemcachedClient.Store(StoreMode.Set, key, serializedValue, validFor);

                // Store Value by Key
                if (isStored)
                {
                    Log.Instance.Info("Store succesfull: " + key);

                    // Add the cached record into a Repository
                    if (!String.IsNullOrEmpty(repository))
                    {
                        CasRepositoryStore(key, repository);
                    }

                    return true;
                }
                else
                {
                    Log.Instance.Fatal("Store failed: " + key);
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Add a Key into a Cas Repository
        /// N.B. Cas records DO NOT exipire, see Enyim inline documentation
        /// </summary>
        /// <param name="key"></param>
        /// <param name="repository"></param>
        private static void CasRepositoryStore(string key, string repository)
        {
            if (String.IsNullOrEmpty(repository))
            {
                return;
            }
            else
            {
                // Force to Case Insensitive
                repository = repository.ToUpper();
            }

            // Initiate Keys
            List<string> keys = new List<string>();

            try
            {
                // Initiate loop
                bool pending = true;
                do
                {
                    // Get list of Keys by Cas per Repository
                    CasResult<List<string>> casCache = MemcachedClient.GetWithCas<List<string>>(repository);

                    // Check if Cas record exists
                    if (casCache.Result != null && casCache.Result.Count > 0)
                    {
                        keys = casCache.Result;
                    }

                    // Append Key
                    keys.Add(key);


                    if (casCache.Cas != 0)
                    {
                        // Try to "Compare And Swap" if the Cas identifier exists
                        CasResult<bool> casStore = MemcachedClient.Cas(StoreMode.Set, repository, keys, casCache.Cas);
                        pending = !casStore.Result;
                    }
                    else
                    {
                        // Create a new Cas record
                        CasResult<bool> casStore = MemcachedClient.Cas(StoreMode.Set, repository, keys);
                        pending = !casStore.Result;
                    }
                } while (pending);

                Log.Instance.Info("Key [" + key + "] added to Repository [" + repository + "]");
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return;
            }
        }

        /// <summary>
        /// Remove all the cached records stored into a Cas Repository
        /// </summary>
        /// <param name="repository"></param>
        public static void CasRepositoryFlush(string repository)
        {
            // Check if it's enabled first
            if (!IsEnabled())
            {
                return;
            }

            if (String.IsNullOrEmpty(repository))
            {
                return;
            }
            else
            {
                // Force to Case Insensitive
                repository = repository.ToUpper();
            }

            // Initiate Keys
            List<string> keys = new List<string>();

            try
            {
                // Initiate loop
                bool pending = true;
                do
                {
                    // Get list of Keys by Cas per Repository
                    CasResult<List<string>> casCache = MemcachedClient.GetWithCas<List<string>>(repository);

                    // Check if Cas record exists
                    if (casCache.Result != null && casCache.Result.Count > 0)
                    {
                        // Get the list of Keys
                        keys = casCache.Result;
                    }

                    // Loop trough the Keys and remove the relative cache entries if any exist
                    foreach (string key in keys)
                    {
                        MemcachedClient.Remove(key);
                    }

                    if (casCache.Cas != 0)
                    {
                        // Try to "Compare And Swap" if the Cas identifier exists
                        CasResult<bool> casStore = MemcachedClient.Cas(StoreMode.Set, repository, new List<string>(), casCache.Cas);
                        pending = !casStore.Result;
                    }
                    else
                    {
                        pending = false;
                    }
                } while (pending);

                Log.Instance.Info("Cas Repository [" + repository + "] flushed");
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return;
            }
        }

        /// <summary>
        /// Get the Value for ADO
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public static MemCachedD_Value Get_ADO(string nameSpace, string procedureName, List<ADO_inputParams> inputParams)
        {
            // Get the Key
            string key = GenerateKey_ADO(nameSpace, procedureName, inputParams);

            // Get Value by Key
            return Get(key);
        }

        /// <summary>
        /// Get the Value for BSO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <returns></returns>
        public static MemCachedD_Value Get_BSO<T>(string nameSpace, string className, string methodName, T inputDTO)
        {
            // Get the Key
            string key = GenerateKey_BSO(nameSpace, className, methodName, inputDTO);

            // Get Value by Key
            return Get(key);
        }

        /// <summary>
        /// Get the Value
        /// </summary>
        /// <typeparam name="MemCachedD_Value"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private static MemCachedD_Value Get(string key)
        {
            MemCachedD_Value value = new MemCachedD_Value();

            // Check if it's enabled first
            if (!IsEnabled())
            {
                return value;
            }

            try
            {
                // Get serialised cache by Key
                string cacheSerialised = MemcachedClient.Get<string>(key);

                if (!String.IsNullOrEmpty(cacheSerialised))
                {
                    Log.Instance.Info("Cache found: " + key);
                    // The value must be deserialised
                    dynamic cache = JsonConvert.DeserializeObject(cacheSerialised);

                    DateTime cacheDateTime = (DateTime)cache.datetime;
                    DateTime cacheExpiresAt = (DateTime)cache.expiresAt;
                    TimeSpan cacheValidFor = (TimeSpan)cache.validFor;

                    Log.Instance.Info("Cache Date: " + cacheDateTime.ToString());
                    Log.Instance.Info("Cache Expires At: " + cacheExpiresAt.ToString());
                    Log.Instance.Info("Cache Valid For (s): " + cacheValidFor.TotalSeconds.ToString());
                    Log.Instance.Info("Cache Has Data: " + cache.hasData.ToString());

                    if (!Convert.ToBoolean(cache.hasData))
                    {
                        Log.Instance.Info("Force removal of cache without data");
                        // Remove the record with no data
                        Remove(key);
                    }

                    // double check the cache record is still valid if not cleared by the garbage collector
                    if (cacheExpiresAt > DateTime.Now
                    || cacheDateTime.AddSeconds(cacheValidFor.TotalSeconds) > DateTime.Now)
                    {
                        // Set properties
                        value.datetime = cacheDateTime;
                        value.expiresAt = cacheExpiresAt;
                        value.validFor = cacheValidFor;
                        value.hasData = Convert.ToBoolean(cache.hasData);
                        value.data = cache.data;

                        return value;
                    }
                    else
                    {
                        Log.Instance.Info("Force removal of expired cache");
                        // Remove the expired record
                        Remove(key);
                    }
                }
                else
                {
                    Log.Instance.Info("Cache not found: " + key);
                }

            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
            }

            return value;
        }

        /// <summary>
        /// Remove the record for ADO
        /// </summary
        /// <param name="nameSpace"></param>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public static bool Remove_ADO(string nameSpace, string procedureName, List<ADO_inputParams> inputParams)
        {
            // Get the Key
            string key = GenerateKey_ADO(nameSpace, procedureName, inputParams);

            return Remove(key);
        }

        /// <summary>
        /// Remove the record for BSO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="inputDTO"></param>
        /// <returns></returns>
        public static bool Remove_BSO<T>(string nameSpace, string className, string methodName, T inputDTO)
        {
            // Get the Key
            string key = GenerateKey_BSO(nameSpace, className, methodName, inputDTO);

            return Remove(key);
        }

        /// <summary>
        /// Remove the record by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool Remove(string key)
        {
            // Chekc if it's enabled first
            if (!IsEnabled())
            {
                return false;
            }

            try
            {
                // Remove the record by the Key
                if (MemcachedClient.Remove(key))
                {
                    Log.Instance.Info("Removal succesfull: " + key);
                    return true;
                }
                else
                {
                    Log.Instance.Info("Removal failed: " + key);
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Remove all records
        /// </summary>
        public static void FlushAll()
        {
            // Chekc if it's enabled first
            if (IsEnabled())
            {
                try
                {
                    // Remove all records
                    MemcachedClient.FlushAll();

                    Log.Instance.Info("Flush completed");
                }
                catch (Exception e)
                {
                    Log.Instance.Fatal(e);
                }
            }

        }

        /// <summary>
        /// Set the Value object
        /// </summary>
        /// <param name="data"></param>
        /// <param name="expiresAt"></param>
        /// <param name="validFor"></param>
        /// <returns></returns>
        private static MemCachedD_Value SetValue(dynamic data, DateTime expiresAt, TimeSpan validFor)
        {
            MemCachedD_Value value = new MemCachedD_Value();

            try
            {
                value.datetime = DateTime.Now;
                value.expiresAt = expiresAt;
                value.validFor = validFor;
                value.hasData = true;
                value.data = data;

                return value;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return value;
            }
        }

        /// <summary>
        /// Validate the Expiry parameters
        /// </summary>
        /// <param name="expiresAt"></param>
        /// <param name="validFor"></param>
        private static bool ValidateExpiry(ref DateTime expiresAt, ref TimeSpan validFor)
        {
            if (expiresAt == new DateTime(0))
            {
                // Set Max if expiresAt is Default DateTime
                expiresAt = DateTime.Now.Add(maxTimeSpan);
            }
            if (validFor.TotalSeconds == 0)
            {
                // Set Max if validFor is 0
                validFor = maxTimeSpan;
            }

            // Cache cannot be in the past
            if (expiresAt < DateTime.Now
                || validFor.Ticks < 0)
            {
                Log.Instance.Info("WARNING: Cache Validity cannot be in the past");
                return false;
            }

            if (expiresAt > DateTime.Now.Add(maxTimeSpan))
            {
                // Override the TimeSpan with the max validity if it exceeds the max length allowed by MemCacheD
                Log.Instance.Info("WARNING: Cache Validity reduced to max 30 days (2592000 sec)");
                expiresAt = DateTime.Now.Add(maxTimeSpan);
            }

            if (validFor > maxTimeSpan)
            {
                // Override the TimeSpan with the max validity if it exceeds the max length allowed by MemCacheD
                Log.Instance.Info("WARNING: Cache Validity reduced to max 30 days (2592000 sec)");
                validFor = maxTimeSpan;
            }

            // Get the minimum between expiresAt and NOW + validFor
            if (expiresAt < DateTime.Now.Add(validFor))
            {
                // Calculate the expiry time from now 
                validFor = expiresAt.Subtract(DateTime.Now);
            }
            else
            {
                // Calculate the datetime to expires at from now
                expiresAt = DateTime.Now.Add(validFor);
            }

            return true;
            #endregion
        }
    }

    /// <summary>
    /// Define the Value object
    /// </summary>
    public class MemCachedD_Value
    {
        #region Properties
        /// <summary>
        /// Timestamp of the cache
        /// </summary>
        public DateTime datetime { get; set; }

        /// <summary>
        /// Expiry date
        /// </summary>
        public DateTime expiresAt { get; set; }

        /// <summary>
        /// Validity
        /// </summary>
        public TimeSpan validFor { get; set; }

        /// <summary>
        /// Flag to indicate if the cache has data
        /// </summary>
        public bool hasData { get; set; }

        /// <summary>
        /// Cached data
        /// </summary>
        public dynamic data { get; set; }

        #endregion

        /// <summary>
        /// Cache output
        /// </summary>
        public MemCachedD_Value()
        {
            // Set default
            datetime = DateTime.Now;
            expiresAt = new DateTime(0);
            validFor = new TimeSpan(0);
            hasData = false;
            data = null;
        }
    }
}