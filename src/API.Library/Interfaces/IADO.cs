using Microsoft.Data.SqlClient;
using System.Data;

namespace API
{
    public interface IADO : IDisposable
    {
        //the below constructors is how the ADO is implemented

        /// <summary>
        /// Default constructor to initiate an ADO - SQL Server Connection
        /// connectionName is defaulted to the DefaultConnection if nothing passed
        /// </summary>
        /// <param name="connectionName"></param>

        //public ADO() : this(ApiServicesHelper.ADOSettings.API_ADO_DEFAULT_CONNECTION)
        //{

        //}

        ///// <summary>
        ///// Default constructor to initiate an ADO - SQL Server Connection
        ///// connectionName can be specified
        ///// </summary>
        ///// <param name="connectionName"></param>
        //public ADO(string connectionName)
        //{
        //    OpenConnection(connectionName);
        //}
        void CloseConnection(bool onDispose = false);
        void CommitTransaction();
        void ExecuteBulkCopy(string destinationTableName, List<KeyValuePair<string, string>> mappings, DataTable dt, bool useCurrentTransaction = false, int copyOptions = 0);
        void ExecuteBulkCopy(string destinationTableName, List<SqlBulkCopyColumnMapping> mappings, DataTable dt, bool useCurrentTransaction = false, SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default);
        void ExecuteNonQueryProcedure(string procedureName, List<ADO_inputParams> inputParams, ref ADO_returnParam returnParam);
        void ExecuteNonQueryProcedure(string procedureName, List<ADO_inputParams> inputParams, ref ADO_returnParam returnParam, ref ADO_outputParam outputParam);
        ADO_readerOutput ExecuteReaderProcedure(string procedureName, List<ADO_inputParams> inputParams);

        ///<summary>
        ///allows a command object to be returned for custom data reading
        ///</summary>
        SqlCommand ExecuteCustomReaderProcedureSetup(string procedureName, List<ADO_inputParams> inputParams);

        void OpenConnection(string connectionName);
        void RollbackTransaction();
        void StartTransaction(IsolationLevel transactionIsolation = IsolationLevel.ReadCommitted);
    }
}
