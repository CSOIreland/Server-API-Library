using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
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

        /// <summary>
        /// Active Directory custom properties for Querying
        /// </summary>
        private static List<string> adCustomProperties = (ConfigurationManager.AppSettings["API_AD_CUSTOM_PROPERTIES"]).Split(',').ToList<string>();
        #endregion

        #region Methods
        /// <summary>
        /// This method returns the entire Active Directory list for the configure Domain
        /// </summary>
        /// <returns></returns>
        private static IDictionary<string, dynamic> GetDirectory<T>() where T : UserPrincipal
        {
            Log.Instance.Info("AD Domain: " + adDomain);
            Log.Instance.Info("AD Path: " + adPath);
            Log.Instance.Info("AD Username: " + adUsername);
            Log.Instance.Info("AD Password: ********"); // Hide adPassword from logs

            // new ExpandoObject() and implement the interface for handling dynamic properties
            var adUsers_IDictionary = new ExpandoObject() as IDictionary<string, dynamic>;

            var inputDTO = Utility.JsonSerialize_IgnoreLoopingReference(Activator.CreateInstance(typeof(T), new object[] { new PrincipalContext(ContextType.Domain) }) as T);
            MemCachedD_Value adCache = MemCacheD.Get_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", inputDTO);
            if (adCache.hasData)
                return adCache.data.ToObject<Dictionary<string, dynamic>>();

            try
            {
                // Get to the Domain
                using (var context = new PrincipalContext(ContextType.Domain, adDomain, String.IsNullOrEmpty(adPath) ? null : adPath, String.IsNullOrEmpty(adUsername) ? null : adUsername, String.IsNullOrEmpty(adPassword) ? null : adPassword))
                {
                    // Crete the query filterusing enabled accounts and excluding those with blank basic properties
                    var queryFilter = Activator.CreateInstance(typeof(T), new object[] { context }) as T;
                    queryFilter.Enabled = true;
                    queryFilter.SamAccountName = "*";
                    queryFilter.EmailAddress = "*";
                    queryFilter.GivenName = "*";
                    queryFilter.Surname = "*";

                    // Run the search
                    using (var searcher = new PrincipalSearcher(queryFilter))
                    {
                        // Loop trough the results and sort then by SamAccountName
                        // Cast to dynamic to get all properties including any custom one
                        foreach (var result in searcher.FindAll().Cast<dynamic>().OrderBy(x => x.SamAccountName))
                        {
                            // Check for duplicate accounts
                            if (adUsers_IDictionary.ContainsKey(result.SamAccountName))
                            {
                                // Do not add duplicate users and report the issue
                                Log.Instance.Fatal("A duplicate Username has been found in AD: " + result.SamAccountName + ". Contact the AD Administrator to clean the duplicate username in the relevant AD Domain.");
                            }
                            else
                            {
                                // Create a shallow copy of AD with the mandatory proprieties for caching/serialising it later on
                                var userPrincipal_ShallowCopy = new ExpandoObject() as IDictionary<string, Object>;

                                userPrincipal_ShallowCopy.Add("SamAccountName", result.SamAccountName);
                                userPrincipal_ShallowCopy.Add("UserPrincipalName", result.UserPrincipalName);
                                userPrincipal_ShallowCopy.Add("DistinguishedName", result.DistinguishedName);
                                userPrincipal_ShallowCopy.Add("DisplayName", result.DisplayName);
                                userPrincipal_ShallowCopy.Add("Name", result.Name);
                                userPrincipal_ShallowCopy.Add("GivenName", result.GivenName);
                                userPrincipal_ShallowCopy.Add("MiddleName", result.MiddleName);
                                userPrincipal_ShallowCopy.Add("Surname", result.Surname);
                                userPrincipal_ShallowCopy.Add("EmailAddress", result.EmailAddress);
                                userPrincipal_ShallowCopy.Add("EmployeeId", result.EmployeeId);
                                userPrincipal_ShallowCopy.Add("VoiceTelephoneNumber", result.VoiceTelephoneNumber);
                                userPrincipal_ShallowCopy.Add("Description", result.Description);

                                // Add the cusotm properties to the shallow copy if any
                                foreach (string property in adCustomProperties)
                                {
                                    userPrincipal_ShallowCopy.Add(property, result.GetType().GetProperty(property)?.GetValue(result, null));
                                }

                                // Add user to the dictionary, serialise/deserialise to avoid looping references
                                adUsers_IDictionary.Add(result.SamAccountName, userPrincipal_ShallowCopy as ExpandoObject);
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
            MemCacheD.Store_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", inputDTO, adUsers_IDictionary, DateTime.Today.AddDays(1));

            return adUsers_IDictionary;
        }

        /// <summary>
        /// This method list Active Directory for the configured Domain
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, dynamic> List()
        {
            // Get the full directory
            return List<UserPrincipal>();
        }
        public static IDictionary<string, dynamic> List<T>() where T : UserPrincipal
        {
            // Get the full directory
            return GetDirectory<T>();
        }

        /// <summary>
        /// This method search a user in Active Directory for the configured Domain
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static dynamic Search(string username)
        {
            return Search<UserPrincipal>(username);
        }
        public static dynamic Search<T>(string username) where T : UserPrincipal
        {
            // Get the full director
            IDictionary<string, dynamic> adDirectory = GetDirectory<T>();

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

        /// <summary>
        /// Validate a Password against an AD account
        /// </summary>
        /// <param name="userPrincipal"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool IsPasswordValid(dynamic userPrincipal, string password)
        {
            bool isValid = false;
            using (PrincipalContext pricipalContext = new PrincipalContext(ContextType.Domain, adDomain, String.IsNullOrEmpty(adPath) ? null : adPath, String.IsNullOrEmpty(adUsername) ? null : adUsername, String.IsNullOrEmpty(adPassword) ? null : adPassword))
            {
                // validate the credentials
                isValid = pricipalContext.ValidateCredentials(userPrincipal, password);
            }
            return isValid;
        }
        #endregion

    }

    /// <summary>
    /// Template to implement a extended UserPrincipal to retrieve custom AD properties (i.e. Sample)
    /// </summary>
    /*
    [DirectoryRdnPrefix("CN")]
    [DirectoryObjectClass("Person")]
    public partial class UserPrincipalExtended : UserPrincipal
    {
        public UserPrincipalExtended(PrincipalContext context) : base(context) { }

        
        // Create the "Sample" property.    
        [DirectoryProperty("sample")]
        public string Sample
        {
            get
            {
                if (ExtensionGet("sample").Length != 1)
                    return string.Empty;

                return (string)ExtensionGet("sample")[0];
            }
            set { ExtensionSet("sample", value); }
        }
    }
    */
}
