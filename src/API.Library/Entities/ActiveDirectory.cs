using System;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;

namespace API
{
    /// <summary>
    /// Active Directory
    /// </summary>
    public class ActiveDirectory
    {
        #region Properties
        /// <summary>
        /// Active Directory Domain
        /// </summary>
        private static string adDomain = ConfigurationManager.AppSettings["API_AD_DOMAIN"];

        /// <summary>
        /// Active Directory Path
        /// </summary>
        private static string adPath = ConfigurationManager.AppSettings["API_AD_PATH"];

        /// <summary>
        /// Active Directory Username for Quering 
        /// </summary>
        private static string adUsername = ConfigurationManager.AppSettings["API_AD_USERNAME"];

        /// <summary>
        /// Active Directory Password for Querying 
        /// </summary>
        private static string adPassword = ConfigurationManager.AppSettings["API_AD_PASSWORD"];
        #endregion

        #region Methods
        /// <summary>
        /// This method returns the entire Active Directory list for the configure Domain
        /// </summary>
        /// <returns></returns>
        private static IDictionary<string, dynamic> GetDirectory()
        {
            Log.Instance.Info("AD Domain: " + adDomain);
            Log.Instance.Info("AD Path: " + adPath);
            Log.Instance.Info("AD Username: " + adUsername);
            Log.Instance.Info("AD Password: ********"); // Hide adPassword from logs

            // Initilise a new dynamic object
            dynamic adUsers = new ExpandoObject();
            // Implement the interface for handling dynamic properties
            var adUsers_IDictionary = adUsers as IDictionary<string, dynamic>;

            MemCachedD_Value adCache = MemCacheD.Get_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", null);
            if (adCache.hasData)
                return adCache.data.ToObject<Dictionary<string, dynamic>>();

            try
            {
                // Get to the Domain
                using (var context = new PrincipalContext(ContextType.Domain, adDomain, String.IsNullOrEmpty(adPath) ? null : adPath, String.IsNullOrEmpty(adUsername) ? null : adUsername, String.IsNullOrEmpty(adPassword) ? null : adPassword))
                {
                    // Get to the Search, filtering by Enabled accounts, exclude accounts with blank properties
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context) { Enabled = true, SamAccountName = "*", EmailAddress = "*", GivenName = "*", Surname = "*" }))
                    {
                        // Loop trough the results and sort then by SamAccountName
                        foreach (var result in searcher.FindAll().Cast<UserPrincipal>().OrderBy(x => x.SamAccountName))
                        {
                            // Check for duplicate accounts
                            if (adUsers_IDictionary.ContainsKey(result.SamAccountName))
                            {
                                // Do not add duplicate users and report the issue
                                Log.Instance.Fatal("A duplicate Username has been found in AD: " + result.SamAccountName + ". Contact the AD Administrator to clean the duplicate username in the relevant AD Domain.");
                            }
                            else
                            {
                                // Create a shallow copy of the UserPrincipal with the main proprieties for caching/serialising it later on
                                dynamic userPrincipal_ShallowCopy = new ExpandoObject();
                                userPrincipal_ShallowCopy.SamAccountName = result.SamAccountName;
                                userPrincipal_ShallowCopy.UserPrincipalName = result.UserPrincipalName;
                                userPrincipal_ShallowCopy.DistinguishedName = result.DistinguishedName;
                                userPrincipal_ShallowCopy.DisplayName = result.DisplayName;
                                userPrincipal_ShallowCopy.Name = result.Name;
                                userPrincipal_ShallowCopy.GivenName = result.GivenName;
                                userPrincipal_ShallowCopy.MiddleName = result.MiddleName;
                                userPrincipal_ShallowCopy.Surname = result.Surname;
                                userPrincipal_ShallowCopy.EmailAddress = result.EmailAddress;
                                userPrincipal_ShallowCopy.EmployeeId = result.EmployeeId;
                                userPrincipal_ShallowCopy.VoiceTelephoneNumber = result.VoiceTelephoneNumber;
                                userPrincipal_ShallowCopy.Description = result.Description;

                                // Add user to the dictionary, serialise/deserialise to avoid looping references
                                adUsers_IDictionary.Add(result.SamAccountName, userPrincipal_ShallowCopy);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Invalid AD configuration");
                Log.Instance.Fatal(e);
            }

            // Set the cache to expire at midnight
            MemCacheD.Store_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", null, adUsers_IDictionary, DateTime.Today.AddDays(1));

            return adUsers_IDictionary;
        }

        /// <summary>
        /// This method list Active Directory for the configured Domain
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, dynamic> List()
        {
            // Get the full directory
            return GetDirectory();
        }

        /// <summary>
        /// This method search a user in Active Directory for the configured Domain
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static dynamic Search(string username)
        {
            // Get the full director
            IDictionary<string, dynamic> adDirectory = GetDirectory();

            if (adDirectory.ContainsKey(username))
                return adDirectory[username];
            else
                return null;
        }

        /// <summary>
        /// Checks if the user is authenticated against Active Directory
        /// </summary>
        /// <param name="userPrincipal"></param>
        /// <returns></returns>
        public static bool IsAuthenticated(dynamic userPrincipal)
        {
            if (userPrincipal == null || String.IsNullOrEmpty(userPrincipal.SamAccountName.ToString()))
                return false;
            else
                return true;
        }
        #endregion

    }

}