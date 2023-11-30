using Microsoft.Data.SqlClient;
using System.Data;

namespace API
{
    /// <summary>
    /// ADO classes for CacheTrace
    /// </summary>
    internal static class CacheTrace_ADO
    {
        /// <summary>
        /// Creates a Trace
        /// </summary>
        /// <param name="ado"></param>
        /// <param name="trace"></param>
        /// <param name="inTransaction"></param>
        /// <returns></returns>
        internal static void Create(DataTable cacheTable)
        {
            if (!ApiServicesHelper.CacheConfig.API_CACHE_TRACE_ENABLED)
            {
             return;
            }

            ADO ado = string.IsNullOrEmpty(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE) ? new ADO() : new ADO(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE);

            try
            {           
                var mapping = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < cacheTable.Columns.Count; i++)
                {
                  mapping.Add(new KeyValuePair<string, string>(cacheTable.Columns[i].ColumnName, cacheTable.Columns[i].ColumnName));
                }
                ado.ExecuteBulkCopy("TD_API_CACHE_TRACE", mapping, cacheTable);
            }
            catch (Exception ex)
            {
                Log.Instance.Fatal("Error storing cache trace information : " + ex + "");
            }
            finally
            {
                ado.CloseConnection();
                ado.Dispose();
            }
        }
    }
}
