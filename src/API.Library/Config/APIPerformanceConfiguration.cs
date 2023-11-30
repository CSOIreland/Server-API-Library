using Microsoft.Extensions.Options;

namespace API
{

    public class APIPerformanceConfiguration : IAPIPerformanceConfiguration
    {
        internal static IOptionsMonitor<APIPerformanceSettings> _APIPerformanceSettingsDelegate;
        public APIPerformanceConfiguration(IOptionsMonitor<APIPerformanceSettings> APIPerformanceSettingsDelegate)
        {
            _APIPerformanceSettingsDelegate = APIPerformanceSettingsDelegate;
        }

        public bool API_PERFORMANCE_ENABLED
        {
            get
            {
                return _APIPerformanceSettingsDelegate.CurrentValue.API_PERFORMANCE_ENABLED;
            }
        }

    }
}