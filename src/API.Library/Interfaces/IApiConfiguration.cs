using System.DirectoryServices.AccountManagement;

namespace API
{
    public interface IApiConfiguration
    {
        IDictionary<string, string> Settings { get; }
        bool MAINTENANCE { get; }

        decimal? version { get; }

        void Refresh();

        bool API_TRACE_RECORD_IP { get; }

        bool API_TRACE_ENABLED { get; }
    }
}
