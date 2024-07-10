using System.Diagnostics;
using System.Net;

namespace API
{
    internal class PerformanceObj
    {
        internal string Server = Dns.GetHostName();
        internal DateTime DateTime { get; set; }
        internal int ProcessorPercentage { get; set; }
        internal int MemoryAvailableMBytes { get; set; }
        internal int RequestPerSec { get; set; }
        internal int RequestsQueued { get; set; }
    }

    internal class PerformanceCollector : IDisposable
    {

        internal List<PerformanceObj> items = new List<PerformanceObj>();

        internal void CollectData(CancellationToken cancelToken)
        {

            /// <summary>
            /// Flag to indicate if Performance is enabled 
            /// </summary>
            bool API_PERFORMANCE_ENABLED = ApiServicesHelper.APIPerformanceSettings.API_PERFORMANCE_ENABLED;

            Log.Instance.Info("Performance Enabled: " + API_PERFORMANCE_ENABLED);

            if (!API_PERFORMANCE_ENABLED)
            {
                return;
            }

            PerformanceCounter ProcessorPercentage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter MemoryAvailableMBytes = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter RequestPerSec = new PerformanceCounter("ASP.NET Applications", "Requests/Sec", "__Total__");
            PerformanceCounter RequestsQueued = new PerformanceCounter("ASP.NET", "Requests Queued");

            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    PerformanceObj performanceObj = new PerformanceObj();

                    float processorPercentage = ProcessorPercentage.NextValue();
                    float memoryAvailableMBytes = MemoryAvailableMBytes.NextValue();
                    float requestPerSec = RequestPerSec.NextValue();
                    float requestsQueued = RequestsQueued.NextValue();

                    performanceObj.ProcessorPercentage = (int)Math.Round(processorPercentage);
                    performanceObj.MemoryAvailableMBytes = (int)Math.Round(memoryAvailableMBytes);
                    performanceObj.RequestsQueued = (int)Math.Round(requestsQueued);
                    performanceObj.RequestPerSec = (int)Math.Round(requestPerSec);
                    performanceObj.DateTime = DateTime.Now.TrimToMinute(TimeSpan.TicksPerMinute);

                    items.Add(performanceObj);

                    // Collect data every second
                    Thread.Sleep(1000);
                }

                if (cancelToken.IsCancellationRequested)
                {
                    Performance_ADO.Create(String.IsNullOrEmpty(ApiServicesHelper.ADOSettings.API_PERFORMANCE_DATABASE) ? new ADO() : new ADO(ApiServicesHelper.ADOSettings.API_PERFORMANCE_DATABASE), items, ApiServicesHelper.APIPerformanceSettings.API_PERFORMANCE_ENABLED);
                }
            }
            catch (ThreadAbortException e)
            {
                // Thread aborted, do nothing
                // Dispose will take care of everything safely
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
            }
            finally
            {
                Dispose();
            }

        }

        /// <summary>
        /// Dispose() calls Dispose(true)
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
