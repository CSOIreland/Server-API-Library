namespace API
{
    /// <summary>
    /// Collection of constants
    /// </summary>
    public static class APIConstants
    {
        /// <summary>
        /// Windows Authentication constant
        /// </summary>
        public const string AUTHENTICATION_TYPE_WINDOWS = "WINDOWS";

        /// <summary>
        /// Anonymous Authentication constant
        /// </summary>
        public const string AUTHENTICATION_TYPE_ANONYMOUS = "ANONYMOUS";

        /// <summary>
        /// Any WINDOWS Authentication constant
        /// </summary>
        public const string AUTHENTICATION_TYPE_ANY_WINDOWS = "ANY_WINDOWS";

        /// <summary>
        /// Any ENTRAID Authentication constant
        /// </summary>
        public const string AUTHENTICATION_TYPE_ANY_ENTRAID = "ANY_ENTRAID";

        /// <summary>
        /// Any Authentication constant
        /// </summary>
        public const string AUTHENTICATION_TYPE_ENTRAID = "ENTRAID";

        /// <summary>
        /// UserPrincipal container in Session
        /// </summary>
        public const string UserPrincipal_Container = "UserPrincipal";

        /// <summary>
        /// attribute used for identity within the API
        /// </summary>
        public const string API_IDENTITY = "upn";


        /// <summary>
        /// attribute used for identity within the API when an external ENTRAID token that is a service account
        /// </summary>
        public const string API_IDENTITY_EXTERNAL_SSO_AUTHENTICATION = "appid";

        /// <summary>
        /// columns used for query to entra id 
        /// </summary>
        public const string API_ENTRA_QUERY = "manager,JobTitle,Department,accountEnabled,UserPrincipalName,mail,GivenName,Surname,DisplayName,OnPremisesDistinguishedName,OnPremisesSamAccountNameDepartment,OfficeLocation,onPremisesExtensionAttributes,businessPhones";
    }
} 