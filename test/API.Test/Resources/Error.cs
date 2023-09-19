namespace Sample
{
    /// <summary>
    /// Base Error class based on CRUD to extend/override as needed by the Application
    /// </summary>
    public static class Error
    {
        #region Properties

        /// <summary>
        /// Create
        /// </summary>
        internal static readonly string Create = AppServicesHelper.StaticConfig.APP_ERROR_CREATE;

        // Read (BSO based)

        /// <summary>
        /// Update
        /// </summary>
        internal static readonly string update = AppServicesHelper.StaticConfig.APP_ERROR_UPDATE;

        /// <summary>
        /// Delete
        /// </summary>
        internal static readonly string delete = AppServicesHelper.StaticConfig.APP_ERROR_DELETE; 
        #endregion
    }
}