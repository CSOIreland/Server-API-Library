using System.Data;

namespace API
{
    internal static class Performance_ADO
    {

        internal static void Create(ADO ado, List<PerformanceObj> performanceList, bool api_performance_enabled)
        {
            try
            {
                
                if (!api_performance_enabled)
                {
                    return;
                }

                Log.Instance.Info("Performance Records Collected: " + performanceList.Count.ToString());

                DataTable performanceTable = new DataTable();
                performanceTable.Columns.Add("PRF_PROCESSOR_PERCENTAGE");
                performanceTable.Columns.Add("PRF_MEMORY_AVAILABLE");
                performanceTable.Columns.Add("PRF_REQUEST_QUEUE");
                performanceTable.Columns.Add("PRF_REQUEST_PERSECOND");
                performanceTable.Columns.Add("PRF_DATETIME");
                performanceTable.Columns.Add("PRF_SERVER");

                foreach (var p in performanceList)
                {
                    performanceTable.Rows.Add(new object[] { p.ProcessorPercentage, p.MemoryAvailableMBytes, p.RequestsQueued, p.RequestPerSec, p.DateTime, p.Server });
                }

                var mapping = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < performanceTable.Columns.Count; i++)
                {
                    mapping.Add(new KeyValuePair<string, string>(performanceTable.Columns[i].ColumnName, performanceTable.Columns[i].ColumnName));
                }

         
                ado.ExecuteBulkCopy("TD_PERFORMANCE", mapping, performanceTable);
            }
            catch (Exception ex)
            {
                Log.Instance.Error("Error storing performance information");
                Log.Instance.Error(ex);
            }
            finally
            {
                ado.Dispose();
            }
        }
    }
}
