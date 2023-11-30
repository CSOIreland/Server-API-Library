using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;

namespace API
{
    /// <summary>
    /// ADO classes for CacheTrace
    /// </summary>
    internal static class DatabaseTrace_ADO
    {
        /// <summary>
        /// Creates a Trace
        /// </summary>
        /// <param name="ado"></param>
        /// <param name="trace"></param>
        /// <param name="inTransaction"></param>
        /// <returns></returns>
        internal static void Create(DataTable databaseTraceTable)
        {
            if (!ApiServicesHelper.DatabaseTracingConfiguration.API_DATABASE_TRACE_ENABLED)
            {
                return;
            }

            ADO ado = string.IsNullOrEmpty(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE) ? new ADO() : new ADO(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE);

            try
            {
                var mapping = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < databaseTraceTable.Columns.Count; i++)
                {
                    mapping.Add(new KeyValuePair<string, string>(databaseTraceTable.Columns[i].ColumnName, databaseTraceTable.Columns[i].ColumnName));
                }
                ado.ExecuteBulkCopy("TD_API_DATABASE_TRACE", mapping, databaseTraceTable);

            }
            catch (Exception ex)
            {
                Log.Instance.Fatal("Error storing database trace information : " + ex + "");
            }
            finally
            {
                ado.CloseConnection();
                ado.Dispose();
            }
        }
    }
}
