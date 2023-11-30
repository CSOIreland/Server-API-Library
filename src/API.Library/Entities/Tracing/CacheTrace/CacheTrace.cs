using System.Data;
using System.Data.SqlTypes;

namespace API
{
    /// <summary>
    /// CacheTrace
    /// </summary>
    internal class CacheTrace
    {
        internal static DataTable CreateCacheTraceDataTable()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("CCH_CORRELATION_ID");
            dataTable.Columns.Add("CCH_OBJECT");
            dataTable.Columns.Add("CCH_START_TIME");
            dataTable.Columns.Add("CCH_DURATION", typeof(SqlDecimal));
            dataTable.Columns.Add("CCH_ACTION");
            dataTable.Columns.Add("CCH_SUCCESS");
            dataTable.Columns.Add("CCH_COMPRESSED_SIZE");
            dataTable.Columns.Add("CCH_EXPIRES_AT");
            return dataTable;
        }


        //adds rows to cacheTraceDataTable asynclocal variable
        internal static void PopulateCacheTrace(string cacheObject, DateTime startTime, decimal duration, string action, bool success, int? compressed_size, DateTime? expiresAt)
        {
            if (ApiServicesHelper.ApiConfiguration != null && APIMiddleware.correlationID.Value != null)
            {
                if (ApiServicesHelper.CacheConfig.API_CACHE_TRACE_ENABLED )
                {
                    APIMiddleware.cacheTraceDataTable.Value.Rows.Add(APIMiddleware.correlationID.Value, cacheObject, startTime, duration, action, success, compressed_size, expiresAt);
                }
            }
           
        }
    }
}
