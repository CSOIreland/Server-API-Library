using System.Net;

namespace API
{ 
    public interface IResponseOutput
    {
        dynamic data { get; set; }
        dynamic response { get; set; }
        dynamic error { get; set; }
        Cookie sessionCookie { get; set; }
        string mimeType { get; set; }
        HttpStatusCode statusCode { get; set; }
        string fileName { get; set; }
    }
}
