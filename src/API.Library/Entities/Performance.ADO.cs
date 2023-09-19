using Microsoft.Data.SqlClient;
using System.Data;

namespace API
{
    internal static class Performance_ADO
    {

        internal static void Create(ADO ado, List<PerformanceObj> performanceList, bool api_performance_enabled)
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

            List<SqlBulkCopyColumnMapping> tableMap = new List<SqlBulkCopyColumnMapping>()
                {
                    new SqlBulkCopyColumnMapping("PRF_PROCESSOR_PERCENTAGE", "PRF_PROCESSOR_PERCENTAGE"),
                    new SqlBulkCopyColumnMapping("PRF_MEMORY_AVAILABLE", "PRF_MEMORY_AVAILABLE"),
                    new SqlBulkCopyColumnMapping("PRF_REQUEST_QUEUE", "PRF_REQUEST_QUEUE"),
                    new SqlBulkCopyColumnMapping("PRF_REQUEST_PERSECOND", "PRF_REQUEST_PERSECOND"),
                    new SqlBulkCopyColumnMapping("PRF_DATETIME", "PRF_DATETIME"),
                    new SqlBulkCopyColumnMapping("PRF_SERVER", "PRF_SERVER")
                };

            // No transaction required
            ado.ExecuteBulkCopy("TD_PERFORMANCE", tableMap, performanceTable);

            ado.Dispose();
        }
    }
}
