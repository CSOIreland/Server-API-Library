using AngleSharp.Css.Dom;
using Ganss.Xss;
using System.Reflection;

namespace API
{
    public interface ISanitizer
    {
        /// <summary>
        /// Searches an object's properties and if it finds a property with a specific custom attribute, it performs an operation on the value of the property
        /// Also performs some default sanitizations.
        /// </summary>
        /// <param name="DTO"></param>
        /// <returns></returns>

        public dynamic Sanitize(dynamic DTO);
  

        /// <summary>
        /// Removes certain values from the string
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="pvalue"></param>
        /// <returns></returns>

        public string CleanValue(PropertyInfo propertyInfo, string pvalue);
        
        /// Gets or sets the allowed tag names such as "a" and "div".
        /// </summary>
        public ISet<string> AllowedTags { get; set; }

        /// <summary>
        /// Gets or sets the allowed HTML attributes such as "href" and "alt".
        /// </summary>
        public ISet<string> AllowedAttributes { get; set; }

        /// <summary>
        /// Gets or sets the allowed CSS classes.
        /// </summary>
        public ISet<string> AllowedCssClasses { get; set; }

        /// <summary>
        /// Gets or sets the allowed CSS properties such as "font" and "margin".
        /// </summary>
        public ISet<string> AllowedCssProperties { get; set; } 

        /// <summary>
        /// Gets or sets the allowed CSS at-rules such as "@media" and "@font-face".
        /// </summary>
        public ISet<CssRuleType> AllowedAtRules { get; set; } 

        /// <summary>
        /// Gets or sets the allowed URI schemes such as "http" and "https".
        /// </summary>
        public ISet<string> AllowedSchemes { get; set; } 

        /// <summary>
        /// Gets or sets the HTML attributes that can contain a URI such as "href".
        /// </summary>
        public ISet<string> UriAttributes { get; set; }

    }
}
