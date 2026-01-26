using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;
using System.Dynamic;
using System.Diagnostics;

namespace API
{
    /// <summary>
    /// GraphAPI implements IExternalUserData to expose business logic for retrieving user data from Microsoft Graph.
    /// It uses the IGraphClientWrapper abstraction to interact with the Graph SDK.
    /// Using the wrapper decouples the business logic from the SDK, making the code easier to test and maintain.
    /// </summary>
    public class GraphAPI : IExternalUserData
    {
        // The injected IGraphClientWrapper is used to obtain user data.
        private GraphServiceClient _graphServiceClient = null;

        /// <summary>
        /// Parameterless constructor for production use.
        /// </summary>
        public GraphAPI(){
        }

        /// <summary>
        /// alows usage of graphclient that is already configured
        /// </summary>
        /// <returns>A configured instance of GraphServiceClient.</returns>
        public GraphServiceClient getGraphClient()
        {
            if (_graphServiceClient == null)
            {
                _graphServiceClient =  getGraphClientStatic();
                return _graphServiceClient;
            }
            else
            {
                return _graphServiceClient;
            }
        }

        
        /// <summary>
        /// Helper method to create a GraphServiceClient instance.
        /// This method obtains necessary credentials from configuration and sets up the client for production use.
        /// </summary>
        /// <returns>A configured instance of GraphServiceClient.</returns>
        private static GraphServiceClient getGraphClientStatic()
        {
              var timeout = Convert.ToInt32(ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_EXTERNAL_CALL_TIMEOUT"]);
            
              // Retrieve configuration values required for authentication.
              var tenantId = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];
              var clientId = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTID"];
              var clientSecret = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTSECRET"];
                 
              // Create a credential using the client secret.
              var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

              // Create GraphServiceClient instance using the credential.
              var graphClient = new GraphServiceClient(clientSecretCredential);

               // Return the GraphServiceClient
               return graphClient;
        }


        /// <summary>
        /// Retrieves a collection of groups from Microsoft Graph.
        /// It uses the injected wrapper to fetch all group members (handling pagination internally)
        /// and then maps each member to a dictionary containing selected properties.
        /// The final result is a dictionary with a key UserPrincipalName of a user, mapping to a list of user detail dictionaries.
        /// </summary>
        /// <returns>A Task that returns a dictionary with user data.</returns>
        public IDictionary<string, UserDetail> GetGroupMembers()
        {

            GraphServiceClient graphServiceClient = getGraphClient();
         

            Stopwatch sw = new Stopwatch();
               sw.Start();
               try
               {

                   dynamic inputDTO = new ExpandoObject();
                   inputDTO.tenant = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];
                   inputDTO.customProperties = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CUSTOM_PROPERTIES"];
                   inputDTO.groups = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_GROUPS"];

                //check if in cache
                //check cache for entry first otherwise construct the object
                MemCachedD_Value adCache = ApiServicesHelper.CacheD.Get_BSO_WITHLOCK<dynamic>("API", "EntraID", "GetGroupMembers", Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)));

                if (adCache.hasData)
                {
                    return adCache.data.ToObject<Dictionary<string, UserDetail>>();
                   };


                //remove dups and empty names
                var result = ApiServicesHelper.GraphClientWrapper.GetGroupMembersAsync(ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_GROUPS"]
                   .Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(name => name.Trim())
                   .Where(name => !string.IsNullOrEmpty(name))
                   .ToArray()
                   ).Result
                   .GroupBy(u => u.Id)
               .Select(g => g.First())
               .ToDictionary(u => u.UserPrincipalName, u => populateUserDetail(u));

            // Set the cache to expire at midnight
               ApiServicesHelper.CacheD.Store_BSO<dynamic>("API", "EntraID", "GetGroupMembers", Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)), result, DateTime.Today.AddDays(1));

               return result;
           }
           catch (Exception ex)
           {
               Log.Instance.Error($"Error: {ex.Message}");
               if (ex.InnerException != null)
               {
                  Log.Instance.Error($"Details: {ex.InnerException.Message}");
               }
               return new Dictionary<string, UserDetail>(); // Return empty dictionary instead of null
           }
           finally
           {
               sw.Stop();
               Log.Instance.Info("GRAPH TIME : " + Utility.StopWatchToSeconds(sw));
           }
        }

        /// <summary>
        /// Retrieves user details for a specific email.
        /// It uses the wrapper to get the user data and converts it into a dictionary of properties.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>A Task that returns a dictionary of user properties if found; otherwise, null.</returns>
        public UserDetail GetUserByEmail(string email)
        {
            GraphServiceClient _ = getGraphClient();

            try
            {
                  // Use the wrapper to retrieve the user by email.
                  User user = ApiServicesHelper.GraphClientWrapper.GetUserByEmailAsync(email).Result;
                  // Convert the user object to a dictionary if it exists.
                  return user != null ? populateUserDetail(user) : null;
              }
              catch (Exception ex)
              {
                  // Log any errors encountered during the API call.
                  Log.Instance.Fatal(ex);
                  return null;
              }
        }


        /// <summary>
        /// will search for a user from a dictionary of all users
        /// </summary>
        /// <param name="upn">the upn to search.</param>
        /// <returns>userdetail object</returns>
        public UserDetail Search(string upn)
        {
            // Get the full directory
           IDictionary<string, UserDetail> adDirectory = GetGroupMembers();

            if (adDirectory.ContainsKey(upn))
                return adDirectory[upn];
            else
                return null;            
        }

        /// <summary>
        /// Converts a User object to a dictionary containing selected properties.
        /// Reflection is used to extract only those properties defined in the specificProperties list.
        /// This approach allows the method to work even if the underlying User model changes.
        /// </summary>
        /// <param name="userDetail">The User object to process.</param>
        /// <returns>A dictionary with keys corresponding to the selected user properties.</returns>
        private UserDetail populateUserDetail(User user)
        {
            //create additional Properites:
            var customAttributeString = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CUSTOM_PROPERTIES"].Split(',').Select(attr => attr.Trim()).ToList();

            // Create a dynamic object to hold the attributes
            var userAttributes = new ExpandoObject() as IDictionary<string, object>;

            // Loop through the attributes and add them to the dynamic object
            foreach (var attribute in customAttributeString)
            {


                var value = GetNestedPropertyValue(user, attribute);
                if (value != null)
                {
                    userAttributes.Add(attribute, value);
                }

            }

            User manager = null;

            string managerUserPrincipalName = null;

            if (user.Manager != null)
            {

                manager = user.Manager as Microsoft.Graph.Models.User;
                managerUserPrincipalName = manager.UserPrincipalName;
            }


            var userDetail = new UserDetail
                {
                    Identity = user.UserPrincipalName ?? string.Empty,
                    SamAccountName = user.OnPremisesSamAccountName ?? string.Empty,
                    UserPrincipalName = user.UserPrincipalName ?? string.Empty,
                    DistinguishedName = user.OnPremisesDistinguishedName ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty,
                    Name = user.DisplayName ?? string.Empty,
                    GivenName = user.GivenName ?? string.Empty,
                    MiddleName = string.Empty,
                    Surname = user.Surname ?? string.Empty,
                    EmailAddress = user.Mail ?? string.Empty,
                    EmployeeId = string.Empty,
                    Description = user.Department ?? string.Empty,
                    Enabled = user.AccountEnabled,
                    VoiceTelephoneNumber = user.BusinessPhones.FirstOrDefault() ?? string.Empty,
                    JobTitle = user.JobTitle ?? string.Empty,
                    ManagerUserPrincipalName = managerUserPrincipalName ?? string.Empty,
                    AdditionalProperties = userAttributes
                };
                return userDetail;
        }

        private object GetNestedPropertyValue(object obj, string propertyName)
        {
            foreach (var part in propertyName.Split('.'))
            {
                if (obj == null) return null;

                var type = obj.GetType();
                var info = type.GetProperty(part);
                if (info == null)
                {
                    // Check if the property is an extension attribute
                    if (type == typeof(User) && part.StartsWith("ExtensionAttribute"))
                    {
                        var extensionAttributes = obj.GetType().GetProperty("OnPremisesExtensionAttributes")?.GetValue(obj);
                        if (extensionAttributes != null)
                        {
                            var extensionType = extensionAttributes.GetType();
                            var extensionInfo = extensionType.GetProperty(part);
                            if (extensionInfo != null)
                            {
                                return extensionInfo.GetValue(extensionAttributes);
                            }
                        }
                    }
                    return null;
                }
                obj = info.GetValue(obj, null);
            }
            return obj;
        }
    }
}