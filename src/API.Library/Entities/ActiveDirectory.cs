using System.DirectoryServices.AccountManagement;
using System.Dynamic;


namespace API
{
    /// <summary>
    /// Active Directory
    /// </summary>
    public class ActiveDirectory : IActiveDirectory
    {
        #region Properties
        #endregion
        public ActiveDirectory()
        {
        }

        ///// <summary>
        ///// store principal context
        ///// </summary>
        internal PrincipalContext context = null;
      
        #region Methods

        ///// <summary>
        ///// Initiate the Principal context either as AD specified user or application pool user
        ///// </summary>
        public PrincipalContext adContext
        {
            get
            {
                return getContext();
            }
        }

        private PrincipalContext getContext()
        {
            string[] AuthenticationTypeAllowed = new string[]
             {
              APIConstants.AUTHENTICATION_TYPE_WINDOWS,
              APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS
            };

            // Validate the Authentication type

            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Active Directory Not Enabled");
                return null;
            }

            string adUsername = ApiServicesHelper.ApiConfiguration.Settings["API_AD_USERNAME"];
            string adPassword = ApiServicesHelper.ApiConfiguration.Settings["API_AD_PASSWORD"];
            string adDomain = ApiServicesHelper.ApiConfiguration.Settings["API_AD_DOMAIN"];
            string adPath = ApiServicesHelper.ApiConfiguration.Settings["API_AD_PATH"];

            if (!string.IsNullOrEmpty(adUsername) && !string.IsNullOrEmpty(adPassword))
            {
                Log.Instance.Info("AD Domain: " + adDomain);
                Log.Instance.Info("AD Path: " + adPath);
                Log.Instance.Info("AD Username: " + adUsername);
                Log.Instance.Info("AD Password: ********"); // Hide adPassword from logs
                // Define the Domain against the Principal
                context = new PrincipalContext(ContextType.Domain, adDomain, String.IsNullOrEmpty(adPath) ? null : adPath, adUsername, adPassword);
            }
            else
            {
                Log.Instance.Info("AD Domain: " + adDomain);
                Log.Instance.Info("AD Path: " + adPath);
                Log.Instance.Info("AD Login: Authenticate to Active Directory using application pool identity");
                // Define the Domain against the Principal
                context = new PrincipalContext(ContextType.Domain, adDomain, String.IsNullOrEmpty(adPath) ? null : adPath);
            }
            return context;
        }

        /// <summary>
        /// This method returns the entire Active Directory list for the configure Domain
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, UserDetail> GetDirectory<T>() where T : UserPrincipal
        {
            // new ExpandoObject() and implement the interface for handling dynamic properties
            Dictionary<string, UserDetail> adUsers_IDictionary = new Dictionary<string, UserDetail>();


            string[] AuthenticationTypeAllowed = new string[]
             {
               APIConstants.AUTHENTICATION_TYPE_WINDOWS,
               APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS
             };

            // Validate the Authentication type

            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Active Directory Not Enabled");
                return adUsers_IDictionary;
            }


            //OUs that we dont want users from
            List<string> blacklistOUs = (ApiServicesHelper.ApiConfiguration.Settings["API_AD_BLACKLIST_OUS"]).Split(',').ToList<string>();

         

            //create key for cache
            dynamic inputDTO = new ExpandoObject();
            inputDTO.domain = ApiServicesHelper.ApiConfiguration.Settings["API_AD_DOMAIN"];
            inputDTO.path = ApiServicesHelper.ApiConfiguration.Settings["API_AD_PATH"];
            inputDTO.username = ApiServicesHelper.ApiConfiguration.Settings["API_AD_USERNAME"];
            inputDTO.password = ApiServicesHelper.ApiConfiguration.Settings["API_AD_PASSWORD"];
            inputDTO.custom_properties = ApiServicesHelper.ApiConfiguration.Settings["API_AD_CUSTOM_PROPERTIES"];
            inputDTO.blacklist_ous = ApiServicesHelper.ApiConfiguration.Settings["API_AD_BLACKLIST_OUS"];
            inputDTO.type = typeof(T).FullName;

            MemCachedD_Value adCache = ApiServicesHelper.CacheD.Get_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)));
            if (adCache.hasData)
                return adCache.data.ToObject<Dictionary<string, UserDetail>>();
            try
            {
                // Get to the Domain

                using (var context = ApiServicesHelper.ActiveDirectory.adContext)
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
                        // Loop trough the results, exclude on OU's specified and sort then by SamAccountName
                        // Cast to dynamic to get all properties including any custom one
        
                      foreach (var result in searcher.FindAll()
                            .Cast<dynamic>()
                            .OrderBy(x => x.SamAccountName)
                            .OfType<UserPrincipal>()
                            .Where(user => !blacklistOUs.Any(ouName => !string.IsNullOrEmpty(ouName) && user.DistinguishedName.Contains($"OU={ouName}", StringComparison.OrdinalIgnoreCase))))
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

                                //     var userPrincipal_ShallowCopy = CreateAPIUserPrincipalObject(result);
                                Dictionary<string, UserDetail> userPrincipal = ApiServicesHelper.ActiveDirectory.CreateAPIUserPrincipalObject(result);

                                adUsers_IDictionary.Add(result.SamAccountName, userPrincipal[result.SamAccountName]);
                            }
                            }
                        }
                    }

                // Set the cache to expire at midnight
                ApiServicesHelper.CacheD.Store_BSO<dynamic>("API", "ActiveDirectory", "GetDirectory", Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)), adUsers_IDictionary, DateTime.Today.AddDays(1));
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Invalid AD configuration");
                Log.Instance.Fatal(e);
            }

            return adUsers_IDictionary;
        }


        public Dictionary<string, UserDetail> CreateAPIUserPrincipalObject(dynamic userPrincipal)
        {
            //var userPrincipal_ShallowCopy = new ExpandoObject() as IDictionary<string, object>;
            Dictionary<string, UserDetail> users = new Dictionary<string, UserDetail>();
        
            string[] AuthenticationTypeAllowed = new string[]
             {
               APIConstants.AUTHENTICATION_TYPE_WINDOWS,
               APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS
             };

            // Validate the Authentication type

            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Active Directory Not Enabled");
                return users;
            }



            var additionalProperties = new ExpandoObject() as IDictionary<string, object>;

            // Add the custom properties to the shallow copy if any
            foreach (string property in ApiServicesHelper.ApiConfiguration.Settings["API_AD_CUSTOM_PROPERTIES"].Split(','))
            {
                additionalProperties.Add(property, userPrincipal.GetType().GetProperty(property)?.GetValue(userPrincipal, null));
            }

            // Add a User object with multiple properties to the dictionary
            users[userPrincipal.SamAccountName] = new UserDetail(
                userPrincipal.SamAccountName,
                userPrincipal.SamAccountName,
                userPrincipal.UserPrincipalName,
                userPrincipal.DistinguishedName,
                userPrincipal.DisplayName,
                userPrincipal.Name,
                userPrincipal.GivenName,
                userPrincipal.MiddleName,
                userPrincipal.Surname,
                userPrincipal.EmailAddress,
                userPrincipal.EmployeeId,
                userPrincipal.Description,
                userPrincipal.Enabled,
                userPrincipal.VoiceTelephoneNumber,
                null,
                null,
                additionalProperties
                );


            return users;

        }
        /// <summary>
        /// This method list Active Directory for the configured Domain
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, UserDetail> List()
        {
            // Get the full directory
            return List<UserPrincipal>();
        }
        public IDictionary<string, UserDetail> List<T>() where T : UserPrincipal
        {
            // Get the full directory
            return GetDirectory<T>();
        }

        /// <summary>
        /// This method search a user in Active Directory for the configured Domain
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public dynamic Search(string username)
        {
            return Search<UserPrincipal>(username);
        }

        public dynamic Search<T>(string username) where T : UserPrincipal
        {
            string[] AuthenticationTypeAllowed = new string[]
            {
              APIConstants.AUTHENTICATION_TYPE_WINDOWS,
              APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS
            };

            // Validate the Authentication type

            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Active Directory Not Enabled");
                return null;
            }

            // Get the full directory
            IDictionary<string, UserDetail> adDirectory = GetDirectory<T>();

            if (adDirectory.ContainsKey(username))
                return adDirectory[username];
            else
                return null;
        }

        /// <summary>
        /// Validate a Password against an AD account
        /// </summary>
        /// <param name="userPrincipal"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [Obsolete]
        public bool IsPasswordValid(dynamic userPrincipal, string password)
        {
            string[] AuthenticationTypeAllowed = new string[]
            {
               APIConstants.AUTHENTICATION_TYPE_WINDOWS,
               APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS
            };

            // Validate the Authentication type

            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Active Directory Not Enabled");
                return false;
            }

            bool isValid = false;
            using (var pricipalContext = ApiServicesHelper.ActiveDirectory.adContext)
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
