using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Ganss.Xss;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace API
{
    /// <summary>
    /// 
    /// </summary>

    public class Sanitizer : ISanitizer
    {
        public Sanitizer()
        {
        }

        /// Gets or sets the allowed tag names such as "a" and "div".
        /// </summary>
        public ISet<string> AllowedTags { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.AllowedTags, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the allowed HTML attributes such as "href" and "alt".
        /// </summary>
        public ISet<string> AllowedAttributes { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.AllowedAttributes, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the allowed CSS classes.
        /// </summary>
        public ISet<string> AllowedCssClasses { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.AllowedClasses);

        /// <summary>
        /// Gets or sets the allowed CSS properties such as "font" and "margin".
        /// </summary>
        public ISet<string> AllowedCssProperties { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.AllowedCssProperties, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the allowed CSS at-rules such as "@media" and "@font-face".
        /// </summary>
        public ISet<CssRuleType> AllowedAtRules { get; set; } = new HashSet<CssRuleType>(HtmlSanitizerDefaults.AllowedAtRules);

        /// <summary>
        /// Gets or sets the allowed URI schemes such as "http" and "https".
        /// </summary>
        public ISet<string> AllowedSchemes { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.AllowedSchemes, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the HTML attributes that can contain a URI such as "href".
        /// </summary>
        public ISet<string> UriAttributes { get; set; } = new HashSet<string>(HtmlSanitizerDefaults.UriAttributes, StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Searches an object's properties and if it finds a property with a specific custom attribute, it performs an operation on the value of the property
        /// Also performs some default sanitizations.
        /// </summary>
        /// <param name="DTO"></param>
        /// <returns></returns>

        public dynamic Sanitize(dynamic DTO)
        {
            //For all non-null strings we apply some general sanitization rules
            var info = DTO.GetType().GetProperties();

            foreach (PropertyInfo propertyInfo in info)
            {
                string nspace = propertyInfo.PropertyType.Namespace;
                var pvalue = propertyInfo.GetValue(DTO);

                if (propertyInfo.PropertyType.Name.Equals("String"))
                {
                    if (pvalue != null)
                    {
                        var noSanitizeAttribute = propertyInfo.CustomAttributes.Where(x => x.AttributeType.Name == "APIHTMLSanitizer").FirstOrDefault();
          
                        if (noSanitizeAttribute == null)
                        {
                            pvalue = CleanValue(propertyInfo, pvalue);
                        }
                        else
                        {
                            //Do this in case somebody tries to go under the radar by using html codes..
                            //pvalue = pvalue.Replace("&lt;", "<");
                            //pvalue = pvalue.Replace("&gt;", ">");
                            pvalue = HttpUtility.HtmlDecode(pvalue);

                            //set up the sanitzier options object
                            HtmlSanitizerOptions sanitizerOptions = new HtmlSanitizerOptions();

                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_TAGS"].ToString().IsNullOrEmpty())
                            {
                                AllowedTags.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_TAGS"].Split(",").ToList());
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_ATTRIBUTES"].ToString().IsNullOrEmpty())
                            {
                                AllowedAttributes.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_ATTRIBUTES"].Split(",").ToList());
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_CSSCLASSESS"].ToString().IsNullOrEmpty())
                            {
                                AllowedCssClasses.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_CSSCLASSESS"].Split(",").ToList());
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_CSSPROPERTIES"].ToString().IsNullOrEmpty())
                            {
                                AllowedCssProperties.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_CSSPROPERTIES"].Split(",").ToList());
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_ATRULES"].ToString().IsNullOrEmpty())
                            {
                                try
                                {
                                    IEnumerable<CssRuleType> cssRuleTypes = ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_ATRULES"].Split(",").ToList()
                                                                      .Select(s => Enum.Parse(typeof(CssRuleType), s)).Cast<CssRuleType>();
                                    foreach (var ruleType in cssRuleTypes)
                                    {
                                        AllowedAtRules.Remove(ruleType);
                                    }
                                }catch (Exception ex){
                                    Log.Instance.Error("Error Removing AllowedAtRules for sanitizing : " + ex);
                                }
                             
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_SCHEMES"].ToString().IsNullOrEmpty())
                            {
                                AllowedSchemes.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_ALLOWED_SCHEMES"].Split(",").ToList());
                            }
                            if (!ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_URI_ATTRIBUTES"].ToString().IsNullOrEmpty())
                            {
                                UriAttributes.ExceptWith(ApiServicesHelper.ApiConfiguration.Settings["SANITIZER_REMOVE_URI_ATTRIBUTES"].Split(",").ToList());
                            }

                            sanitizerOptions.AllowedTags = AllowedTags;
                            sanitizerOptions.AllowedAttributes = AllowedAttributes;
                            sanitizerOptions.AllowedCssClasses = AllowedCssClasses;
                            sanitizerOptions.AllowedCssProperties = AllowedCssProperties;
                            sanitizerOptions.AllowedAtRules = AllowedAtRules;
                            sanitizerOptions.AllowedSchemes = AllowedSchemes;
                            sanitizerOptions.UriAttributes = UriAttributes;



                            //If we don't sanitize natively then be default we use the HtmlSanitizer library to delete any script tags etc
                            //First iteration - nuke all scripts
                            HtmlSanitizer sanitizer = new HtmlSanitizer();
                            pvalue = sanitizer.Sanitize(pvalue);

                            //Second iteration - remove all other tags but keep their contents
                            sanitizer = new HtmlSanitizer(sanitizerOptions);
                            sanitizer.KeepChildNodes = true;
                            pvalue = sanitizer.Sanitize(pvalue);
                            

                            //Allow end users to see tags instead of codes - the sanitizer will have replaced real signs with html codes
                            //pvalue = pvalue.Replace("\u00A0", " ");
                            //pvalue = pvalue.Replace("&gt;", ">");
                            //pvalue = pvalue.Replace("&amp;", "&");
                            pvalue = HttpUtility.HtmlDecode(pvalue);
                        }
                        //update the input object
                        propertyInfo.SetValue(DTO, pvalue);
                    }
                }

                if (nspace.Equals("System.Collections.Generic"))
                {
                    if (pvalue is List<string> && pvalue != null)
                    {
                        List<string> cleanList = new List<string>();

                        foreach (string s in pvalue)
                        {
                            if (s == null)
                            {
                                // Ignore any null values
                                continue;
                            }
                            string clean = CleanValue(propertyInfo, s);
                            cleanList.Add(clean);
                        }
                        propertyInfo.SetValue(DTO, cleanList);
                    }
                }
            }
            return DTO;
        }

        /// <summary>
        /// Removes certain values from the string
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="pvalue"></param>
        /// <returns></returns>

        public string CleanValue(PropertyInfo propertyInfo, string pvalue)
        {
            var t = propertyInfo.CustomAttributes;
            var attrNoHtmlStrip = propertyInfo.CustomAttributes.Where(CustomAttributeData => CustomAttributeData.AttributeType.Name == "APINoHtmlStrip").FirstOrDefault();
            var attrNoTrim = propertyInfo.CustomAttributes.Where(CustomAttributeData => CustomAttributeData.AttributeType.Name == "APINoTrim").FirstOrDefault();
            var attrLowerCase = propertyInfo.CustomAttributes.Where(CustomAttributeData => CustomAttributeData.AttributeType.Name == "APILowerCase").FirstOrDefault();
            var attrUpperCase = propertyInfo.CustomAttributes.Where(CustomAttributeData => CustomAttributeData.AttributeType.Name == "APIUpperCase").FirstOrDefault();

            pvalue = pvalue.Replace((char)160, (char)32);

            //Strip out all html tags if the NoHtmlStrip attribute is NOT asserted
            if (attrNoHtmlStrip == null)
                pvalue = Regex.Replace(pvalue, @"<.*?>", "");

            if (attrNoTrim == null)
            {
                //left trim - if the NoTrim attribute is NOT asserted
                pvalue = Regex.Replace(pvalue, @"^\s+", "");
                //right trim - if the NoTrim attribute is NOT asserted
                pvalue = Regex.Replace(pvalue, @"\s+$", "");
                //replace double spaces with single ones - if the NoTrim attribute is NOT asserted
                pvalue = Regex.Replace(pvalue, @" {2,}", " ");
            }

            //Force the value to lower case if the LowerCase attribute is asserted
            if (attrLowerCase != null)
                pvalue = pvalue.ToLower();
            //Force the value to upper case if the UpperCase attribute is asserted
            if (attrUpperCase != null)
                pvalue = pvalue.ToUpper();
            return pvalue;
        }    
    }
}