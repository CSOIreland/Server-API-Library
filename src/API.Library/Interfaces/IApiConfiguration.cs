using System.DirectoryServices.AccountManagement;

namespace API
{
    public interface IApiConfiguration
    {
        IDictionary<string, string> Settings { get; }
        bool MAINTENANCE { get; }
    }
}
