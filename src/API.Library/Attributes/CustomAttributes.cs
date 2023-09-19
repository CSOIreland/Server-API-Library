namespace API
{
    /// <summary>
    ///This checks for the AllowAPICall attribute
    ///Asserting this attribute means that a public method can be called by the API
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]

    public class AllowAPICall : Attribute { }

    /// <summary>
    ///This checks for the APINoCleanseDto attribute
    ///Asserting this attribute means that request parameters will not be cleansed
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class APINoCleanseDto : Attribute { }

   
}
