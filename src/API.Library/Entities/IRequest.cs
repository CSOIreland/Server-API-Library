using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.Net;

namespace API
{
    public interface IRequest
    {
        string method { get; set; }
        dynamic parameters { get; set; }
        dynamic userPrincipal { get; set; }
        string ipAddress { get; set; }
        string userAgent { get; set; }

        NameValueCollection httpGET { get; set; }
        string httpPOST { get; set; }

        Cookie sessionCookie { get; set; }

        string requestType { get; set; }

        IHeaderDictionary requestHeaders { get ; set; }

        string scheme { get; set; }
    }
}
