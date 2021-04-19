using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace API
{
    internal static class Performance
    {
        /// <summary>
        /// Flag to indicate if Performance is enabled 
        /// </summary>
        internal static bool API_PERFORMANCE_ENABLED = Convert.ToBoolean(ConfigurationManager.AppSettings["API_PERFORMANCE_ENABLED"]);
        internal static string API_PERFORMANCE_DATABASE = ConfigurationManager.AppSettings["API_PERFORMANCE_DATABASE"];

        internal static PerformanceCounter ProcessorPercentage = API_PERFORMANCE_ENABLED ? new PerformanceCounter("Processor", "% Processor Time", "_Total") : null;
        internal static PerformanceCounter MemoryAvailableMBytes = API_PERFORMANCE_ENABLED ? new PerformanceCounter("Memory", "Available MBytes") : null;
        internal static PerformanceCounter RequestPerSec = API_PERFORMANCE_ENABLED ? new PerformanceCounter("ASP.NET Applications", "Requests/Sec", "__Total__") : null;
        internal static PerformanceCounter RequestsQueued = API_PERFORMANCE_ENABLED ? new PerformanceCounter("ASP.NET", "Requests Queued") : null;

        // Extension method to trim to whole minute
        internal static DateTime TrimToMinute(this DateTime date, long ticks)
        {
            return new DateTime(date.Ticks - (date.Ticks % ticks));
        }
    }

    internal class PerformanceObj
    {
        internal string Server = Dns.GetHostName();
        internal DateTime DateTime { get; set; }
        internal int ProcessorPercentage { get; set; }
        internal int MemoryAvailableMBytes { get; set; }
        internal int RequestPerSec { get; set; }
        internal int RequestsQueued { get; set; }
    }
    internal class PerfomanceCollector : IDisposable
    {
        internal List<PerformanceObj> items = new List<PerformanceObj>();

        internal void CollectData()
        {
            Log.Instance.Info("Performance Enabled: " + Performance.API_PERFORMANCE_ENABLED);

            if (!Performance.API_PERFORMANCE_ENABLED)
            {
                return;
            }

            try
            {
                while (true)
                {
                    PerformanceObj performanceObj = new PerformanceObj();

                    float processorPercentage = Performance.ProcessorPercentage.NextValue();
                    float memoryAvailableMBytes = Performance.MemoryAvailableMBytes.NextValue();
                    float requestPerSec = Performance.RequestPerSec.NextValue();
                    float requestsQueued = Performance.RequestsQueued.NextValue();


                    performanceObj.ProcessorPercentage = (int)Math.Round(processorPercentage);
                    performanceObj.MemoryAvailableMBytes = (int)Math.Round(memoryAvailableMBytes);
                    performanceObj.RequestsQueued = (int)Math.Round(requestsQueued);
                    performanceObj.RequestPerSec = (int)Math.Round(requestPerSec);
                    performanceObj.DateTime = DateTime.Now.TrimToMinute(TimeSpan.TicksPerMinute); ;

                    items.Add(performanceObj);

                    // Collect data every second
                    Thread.Sleep(1000);
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  The bulk of the clean-up code is implemented in Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Store Perfromance
            if (disposing)
            {
                // Store data
                Performance_ADO.Create(String.IsNullOrEmpty(Performance.API_PERFORMANCE_DATABASE) ? new ADO() : new ADO(Performance.API_PERFORMANCE_DATABASE), items);
            }
        }
    }
}
