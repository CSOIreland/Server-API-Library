using System.Collections.Specialized;
using System.Web;

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

        HttpCookie sessionCookie { get; set; }

    }
}
