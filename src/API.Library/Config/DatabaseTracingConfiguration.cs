using Microsoft.Extensions.Options;

namespace API
{

    public class DatabaseTracingConfiguration : IDatabaseTracingConfiguration
    {
        internal static IOptionsMonitor<ADOSettings> _ADOSettingsDelegate;
        public DatabaseTracingConfiguration(IOptionsMonitor<ADOSettings> ADOSettingsDelegate)
        {
            _ADOSettingsDelegate = ADOSettingsDelegate;
        }


        public bool API_DATABASE_TRACE_ENABLED
        {
            get
            {
                return _ADOSettingsDelegate.CurrentValue.API_DATABASE_TRACE_ENABLED;
            }
        }
    }
}