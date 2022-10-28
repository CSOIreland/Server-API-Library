using System.Net;
using System.Web;

namespace API
{
    public interface IResponseOutput
    {
        dynamic data { get; set; }
        dynamic response { get; set; }
        dynamic error { get; set; }
        HttpCookie sessionCookie { get; set; }
        string mimeType { get; set; }
        HttpStatusCode statusCode { get; set; }
        string fileName { get; set; }
    }
}
