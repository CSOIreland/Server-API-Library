namespace API
{

    /// <summary>
    /// If an object contains contains a parameter with the NoHtmlStrip attribute then no HTML tags will be stripped when passed to the Sanitizer
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class APINoHtmlStrip : Attribute { }

    /// <summary>
    /// If an object contains contains a parameter with the NoTrim attribute then no trimming will apply when passed to the Sanitizer
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class APINoTrim : Attribute { }

    /// <summary>
    /// If an object contains a parameter with the LowerCase attribute then the value will be converted to lower case when passed to the Sanitizer
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class APILowerCase : Attribute { }

    /// <summary>
    /// If an object contains a parameter with the UpperCase attribute then the value will be converted to upper case when passed to the Sanitizer
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class APIUpperCase : Attribute { }

    /// <summary>
    /// Sanitize this property using the htmlsanitizer library only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class APIHTMLSanitizer : Attribute { }
}
