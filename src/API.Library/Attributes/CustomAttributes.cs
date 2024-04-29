namespace API
{
    /// <summary>
    ///This checks for the AllowAPICall attribute
    ///Asserting this attribute means that a public method can be called by the API
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]

    public class AllowAPICall : Attribute { }   
}
