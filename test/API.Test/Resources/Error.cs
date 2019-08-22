using API;

namespace Sample
{
    /// <summary>
    /// Base Error class based on CRUD to extend/override as needed by the Application
    /// </summary>
    public static class Error
    {
        #region Properties
        /// <summary>
        /// 
        /// </summary>
        private static string sectionName = "appStatic";

        /// <summary>
        /// Create
        /// </summary>
        internal static readonly string Create = Utility.GetCustomConfig(sectionName, "APP_ERROR_CREATE");

        // Read (BSO based)

        /// <summary>
        /// Update
        /// </summary>
        internal static readonly string update = Utility.GetCustomConfig(sectionName, "APP_ERROR_UPDATE");

        /// <summary>
        /// Delete
        /// </summary>
        internal static readonly string delete = Utility.GetCustomConfig(sectionName, "APP_ERROR_DELETE");
        #endregion
    }
}