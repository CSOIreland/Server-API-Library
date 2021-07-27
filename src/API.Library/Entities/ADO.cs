using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;

namespace API
{
    /// <summary>
    /// ADO to handle SQL Server
    /// https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1063-implement-idisposable-correctly?view=vs-2017
    /// </summary>
    public class ADO : IDisposable
    {
        #region Properties
        /// <summary>
        /// Default DB connection name
        /// </summary>
        internal static string API_ADO_DEFAULT_CONNECTION = ConfigurationManager.AppSettings["API_ADO_DEFAULT_CONNECTION"];

        /// <summary>
        /// Execution timeout
        /// </summary>
        internal static int API_ADO_EXECUTION_TIMEOUT = Convert.ToInt32(ConfigurationManager.AppSettings["API_ADO_EXECUTION_TIMEOUT"]);

        /// <summary>
        /// Bulkcopy timeout
        /// </summary>
        internal static int API_ADO_BULKCOPY_TIMEOUT = Convert.ToInt32(ConfigurationManager.AppSettings["API_ADO_BULKCOPY_TIMEOUT"]);

        /// <summary>
        /// Bulkcopy timeout
        /// </summary>
        internal static int API_ADO_BULKCOPY_BATCHSIZE = Convert.ToInt32(ConfigurationManager.AppSettings["API_ADO_BULKCOPY_BATCHSIZE"]);

        /// <summary>
        /// Initiate SQL Connection
        /// </summary>
        private SqlConnection connection = null;

        /// <summary>
        /// Initiate SQL Transaction
        /// </summary>
        private SqlTransaction transaction = null;
        #endregion

        #region Methods
        /// <summary>
        /// SQL Connection
        /// </summary>
        protected SqlConnection Connection { get { return connection; } }

        /// <summary>
        /// SQL Transaction
        /// </summary>
        protected SqlTransaction Transaction { get { return transaction; } }

        /// <summary>
        /// Default constructor to initiate an ADO - SQL Server Connection
        /// connectionName is defaulted to the defaultConnection if nothing passed
        /// </summary>
        /// <param name="connectionName"></param>
        public ADO() : this(API_ADO_DEFAULT_CONNECTION)
        {
        }

        /// <summary>
        /// Default constructor to initiate an ADO - SQL Server Connection
        /// connectionName can be specified
        /// </summary>
        /// <param name="connectionName"></param>
        public ADO(string connectionName)
        {
            OpenConnection(connectionName);
        }

        /// <summary>
        /// Check if a connection exists
        /// </summary>
        /// <returns></returns>
        private bool CheckConnection()
        {
            // Check if a connection exists
            if (connection != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Open a SQL Server connection
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public void OpenConnection(string connectionName)
        {
            try
            {
                // Check if a connection already exists
                if (CheckConnection())
                {
                    return;
                }

                // Get the Connection String form the associated Name
                string connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;

                Log.Instance.Info("SQL Server Connection Name: " + connectionName);
                Log.Instance.Info("SQL Server Connection String: ********"); // Hide connectionString from logs

                if (string.IsNullOrEmpty(connectionString))
                {
                    Log.Instance.Fatal("Invalid SQL Server Connection String");
                    return;
                }

                // Open a connection
                connection = new SqlConnection(connectionString);
                connection.Open();
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }

        /// <summary>
        /// Close a SQL Server connection
        /// </summary>
        /// <param name="onDispose"></param>
        public void CloseConnection(bool onDispose = false)
        {
            // Check if a connection already exists
            if (CheckConnection())
            {
                // Rollback Transaction
                RollbackTransaction();

                // Close the SQL connection
                connection.Close();

                // Reset the SQL connection
                ResetConnection();

                if (onDispose)
                {
                    Log.Instance.Info("SQL Server Connection disposed");
                }
                else
                {
                    Log.Instance.Info("SQL Server Connection closed");
                }
            }
            else if (!onDispose)
            {
                Log.Instance.Info("No SQL Server Connection to close nor dispose");
            }
        }

        /// <summary>
        /// Reset a SQL Server connection followoing a Closure
        /// </summary>
        private void ResetConnection()
        {
            // Check if a transaction already exists
            if (CheckConnection())
            {
                // Dispose the Connection 
                connection.Dispose();
                // Override the SQL connection
                connection = null;
            }
        }

        /// <summary>
        /// Check if a transaction exists
        /// </summary>
        /// <returns></returns>
        private bool CheckTransaction()
        {
            // Check if a transaction exists
            if (transaction != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Start a SQL Server transaction
        /// Consider setting "Is Read Committed Snapshot On" to TRUE in the Database options for better performance
        /// </summary>
        /// <param name="transactionIsolation"></param>
        public void StartTransaction(IsolationLevel transactionIsolation = IsolationLevel.ReadCommitted)
        {
            // Check if a transaction already exists
            if (!CheckTransaction())
            {
                // Set a new GUID
                string transactionName = Utility.GetMD5(Guid.NewGuid().ToString());

                // Start a local transaction.
                transaction = connection.BeginTransaction(transactionIsolation, transactionName);

                Log.Instance.Info("SQL Server Transaction opened: " + transactionName);
                Log.Instance.Info("SQL Server Transaction isolation: " + transactionIsolation.ToString());
            }
        }

        /// <summary>
        /// Reset a SQL Server transaction following a Commit or Rollback
        /// </summary>
        private void ResetTransaction()
        {
            // Check if a transaction already exists
            if (CheckTransaction())
            {
                // Dispose the Transaction 
                transaction.Dispose();
                // Override the transaction
                transaction = null;
            }
        }

        /// <summary>
        /// Commit a SQL Server transaction
        /// </summary>
        public void CommitTransaction()
        {
            // Check if a transaction already exists
            if (CheckTransaction())
            {
                transaction.Commit();
                Log.Instance.Info("SQL Server Transaction committed");

                ResetTransaction();
            }
        }

        /// <summary>
        /// Rollback a SQL Server transaction
        /// </summary>
        public void RollbackTransaction()
        {
            // Check if a transaction already exists
            if (CheckTransaction())
            {
                transaction.Rollback();
                Log.Instance.Info("SQL Server Transaction rolledback");

                ResetTransaction();
            }
        }

        /// <summary>
        /// Execute a Non Query Procedure (overload)
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <param name="returnParam"></param>
        public void ExecuteNonQueryProcedure(string procedureName, List<ADO_inputParams> inputParams, ref ADO_returnParam returnParam)
        {
            // Initiate null outputParam
            ADO_outputParam outputParam = null;
            // Call the ovrloaded method
            ExecuteNonQueryProcedure(procedureName, inputParams, ref returnParam, ref outputParam);
        }

        /// <summary>
        /// Execute a Non Query Procedure
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <param name="returnParam"></param>
        /// <param name="outputParam"></param>
        public void ExecuteNonQueryProcedure(string procedureName, List<ADO_inputParams> inputParams, ref ADO_returnParam returnParam, ref ADO_outputParam outputParam)
        {
            Log.Instance.Info("Non Query Procedure: " + procedureName);
            Log.Instance.Info("Non Query Procedure Timeout (s): " + API_ADO_EXECUTION_TIMEOUT.ToString());

            // Check the connection exists
            if (!CheckConnection())
            {
                Log.Instance.Fatal("No SQL Server Connection avaialble");
                return;
            }

            // Initiate Stopwatch
            Stopwatch sw = new Stopwatch();

            try
            {
                // Instantiate new Command within the local scope
                SqlCommand command = connection.CreateCommand();

                // Assign connection to command
                command.Connection = connection;
                // Assign timeout to execution
                command.CommandTimeout = API_ADO_EXECUTION_TIMEOUT;
                // Assign transaction to command for a pending local transaction
                command.Transaction = transaction;
                // Set the command to execute a procedure
                command.CommandType = CommandType.StoredProcedure;
                // Set the name of the procedure to call
                command.CommandText = procedureName;

                // Add Input Parameters
                if (inputParams != null && inputParams.Count() > 0)
                {
                    foreach (dynamic inputParam in inputParams)
                    {
                        Log.Instance.Info("Bind Input Parameter: Name[" + inputParam.name + "] Value[" + inputParam.value.ToString() + "]");
                        command.Parameters.AddWithValue(inputParam.name, inputParam.value).Direction = ParameterDirection.Input;
                    }
                }

                // Add Output Parameter (@sample)
                if (outputParam != null)
                {
                    Log.Instance.Info("Bind Output Parameter: Name[" + outputParam.name + "]");
                    command.Parameters.Add(outputParam.name, SqlDbType.Int).Direction = ParameterDirection.Output;
                }

                // Add Return Parameter (@sample)
                if (returnParam != null)
                {
                    Log.Instance.Info("Bind Return Parameter: Name[" + returnParam.name + "]");
                    command.Parameters.Add(returnParam.name, SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                }

                // Start the watch
                sw.Start();

                // Run the Command
                command.ExecuteNonQuery();

                sw.Stop();
                Log.Instance.Info("Non Query Procedure completed (s): " + Math.Round(sw.Elapsed.TotalMilliseconds / 1000, 3).ToString());

                // Pass the Output Parameter as reference 
                if (outputParam != null)
                {
                    Log.Instance.Info("Get Output Parameter: Value[" + outputParam.value.ToString() + "]");
                    outputParam.value = (int)command.Parameters[outputParam.name].Value;
                }

                // Pass the Return Parameter as reference 
                if (returnParam != null)
                {
                    Log.Instance.Info("Get Return Parameter: Value[" + returnParam.value.ToString() + "]");
                    returnParam.value = (int)command.Parameters[returnParam.name].Value;
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }

        /// <summary>
        /// Execute a Bulk Copy
        /// </summary>
        /// <param name="destinationTableName"></param>
        /// <param name="mappings"></param>
        /// <param name="dt"></param>
        /// <param name="useCurrentTransaction"></param>
        /// <param name="copyOptions"></param>
        public void ExecuteBulkCopy(string destinationTableName, List<SqlBulkCopyColumnMapping> mappings, DataTable dt, bool useCurrentTransaction = false, SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            Log.Instance.Info("Bulk Copy to Table " + destinationTableName.ToUpper());
            Log.Instance.Info("Bulk Copy Timeout (s): " + API_ADO_BULKCOPY_TIMEOUT.ToString());
            Log.Instance.Info("Bulk Copy BatchSize: " + API_ADO_BULKCOPY_BATCHSIZE.ToString());
            Log.Instance.Info("Bulk Copy Use Current Transation: " + useCurrentTransaction.ToString());
            Log.Instance.Info("Bulk Copy Options: " + copyOptions.ToString());

            // Check the connection exists
            if (!CheckConnection())
            {
                Log.Instance.Fatal("No SQL Server Connection avaialble");
                return;
            }

            // Initiate Stopwatch
            Stopwatch sw = new Stopwatch();

            try
            {
                // Initiate a new Bulk Copy
                using (var bulkCopy = useCurrentTransaction ? new SqlBulkCopy(connection, copyOptions, transaction) : new SqlBulkCopy(connection, copyOptions, null))
                {
                    // Set the target table
                    bulkCopy.DestinationTableName = destinationTableName;
                    // Set the timeout
                    bulkCopy.BulkCopyTimeout = API_ADO_BULKCOPY_TIMEOUT;
                    // Set the BatchSize
                    bulkCopy.BatchSize = API_ADO_BULKCOPY_BATCHSIZE;

                    // Add the list of mapped columns
                    foreach (var mapping in mappings)
                    {
                        bulkCopy.ColumnMappings.Add(mapping);
                    }

                    // Start the watch
                    sw.Start();

                    // Begin the copy
                    bulkCopy.WriteToServer(dt);

                    sw.Stop();

                    Log.Instance.Info("Bulk Copy Procedure completed (s): " + Math.Round(sw.Elapsed.TotalMilliseconds / 1000, 3).ToString());
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
        }


        /// <summary>
        /// Execute a Reader Procedure supporting multiple resultsets
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public ADO_readerOutput ExecuteReaderProcedure(string procedureName, List<ADO_inputParams> inputParams)
        {
            Log.Instance.Info("Reader Procedure: " + procedureName);
            Log.Instance.Info("Reader Procedure Timeout (s): " + API_ADO_EXECUTION_TIMEOUT.ToString());

            // Initiate the Reader output
            ADO_readerOutput readerOutput = new ADO_readerOutput();

            // Check the connection exists
            if (!CheckConnection())
            {
                Log.Instance.Fatal("No SQL Server Connection avaialble");
                return readerOutput;
            }

            // Initiate Stopwatch
            Stopwatch sw = new Stopwatch();

            try
            {
                // Instantiate new Command within the local scope
                SqlCommand command = connection.CreateCommand();

                // Assign connection to command
                command.Connection = connection;
                // Assign timeout to execution
                command.CommandTimeout = API_ADO_EXECUTION_TIMEOUT;
                // Assign transaction to command for a pending local transaction
                command.Transaction = transaction;
                // Set the command to execute a procedure
                command.CommandType = CommandType.StoredProcedure;
                // Set the name of the procedure to call
                command.CommandText = procedureName;

                // Add Input Parameters
                if (inputParams != null && inputParams.Count() > 0)
                {
                    foreach (dynamic inputParam in inputParams)
                    {
                        Log.Instance.Info("Bind Input Parameter: Name[" + inputParam.name + "] Value[" + inputParam.value.ToString() + "]");
                        SqlParameter param = command.Parameters.AddWithValue(inputParam.name, inputParam.value);
                        param.Direction = ParameterDirection.Input;
                        if (inputParam.typeName != null)
                        {
                            // Allow to pass a specific custom type (i.e. User Defined Data Types or User Defined Table Types)
                            param.TypeName = inputParam.typeName;
                        }
                    }
                }

                // Initialise Size
                int dataSize = 0;
                // Initialise resultSetIndex
                int resultSetIndex = 0;

                // Start the watch
                sw.Start();

                // Run the Command
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.HasRows)
                    {
                        // Set the flag to check quickly the data exists
                        readerOutput.hasData = true;

                        // Initialise resultSet that is incremented for each resultSet
                        readerOutput.meta.Add(new List<dynamic>());
                        readerOutput.data.Add(new List<dynamic>());

                        int rowIndex = 0;
                        while (reader.Read())
                        {
                            // Initialise a new dynamic object
                            dynamic readerData = new ExpandoObject();
                            // Implement the interface for handling dynamic properties
                            var readerData_IDictionary = readerData as IDictionary<string, object>;

                            // Initilise a new dynamic object
                            dynamic readerColumn = new ExpandoObject();
                            // Implement the interface for handling dynamic properties
                            var readerColumn_IDictionary = readerColumn as IDictionary<string, object>;

                            for (int columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                            {
                                // Set Column Name
                                string columnName = reader.GetName(columnIndex).ToString();
                                // Set Column Value
                                dynamic columnValue = reader[columnIndex];
                                // Add Size
                                dataSize += Convert.ToString(columnValue).Length * sizeof(Char);

                                if (rowIndex == 0)
                                {
                                    // Get Meta information
                                    ADO_readerMetadata readerMetadata = new ADO_readerMetadata();
                                    readerMetadata.specificType = reader.GetProviderSpecificFieldType(columnIndex).FullName.ToString();
                                    readerMetadata.dotNetType = reader.GetFieldType(columnIndex).FullName.ToString();
                                    readerMetadata.sqlType = reader.GetDataTypeName(columnIndex).ToString();

                                    // Add the column Metadata
                                    readerColumn_IDictionary.Add(columnName, readerMetadata);
                                }

                                // Add the column to the Data
                                readerData_IDictionary.Add(columnName, columnValue);
                            }

                            if (rowIndex == 0)
                            {
                                // Append the Metadata to the Output once only
                                readerOutput.meta[resultSetIndex].Add(readerColumn_IDictionary);
                            }

                            // Append the Data to the Output for each row
                            readerOutput.data[resultSetIndex].Add(readerData_IDictionary);

                            rowIndex++;
                        }

                        // Move to the next dataset if any
                        reader.NextResult();
                        resultSetIndex++;
                    }
                }

                sw.Stop();

                // Evaluate if more resultset exist
                if (readerOutput.meta.Count == 1 && readerOutput.data.Count == 1)
                {
                    readerOutput.meta = readerOutput.meta[0];
                    readerOutput.data = readerOutput.data[0];
                }

                // Set the time elapsed
                readerOutput.timeInSec = (float)Math.Round(sw.Elapsed.TotalMilliseconds / 1000, 3);
                // Set the size of the data
                readerOutput.sizeInByte = dataSize;

                Log.Instance.Info("Reader Procedure completed (s): " + readerOutput.timeInSec.ToString());
                Log.Instance.Info("Reader Procedure, size (Byte): " + readerOutput.sizeInByte.ToString());
                Log.Instance.Info("Reader Procedure, has data: " + readerOutput.hasData.ToString());

                return readerOutput;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
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
            // free managed resources
            if (disposing)
            {
                // Close the Connection 
                CloseConnection(true);
            }
        }

        #endregion
    }

    /// <summary>
    /// Define the input parameters for a procedure
    /// </summary>
    public class ADO_inputParams
    {
        #region Properties
        /// <summary>
        /// Input name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Input value
        /// </summary>
        public dynamic value { get; set; }

        /// <summary>
        /// Input type
        /// </summary>
        public string typeName { get; set; }

        #endregion
    }

    /// <summary>
    /// Define the output parameter from a procedure
    /// </summary>
    public class ADO_outputParam
    {
        #region Properties
        /// <summary>
        /// Output name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Output value
        /// </summary>
        public int value { get; set; }

        #endregion
    }

    /// <summary>
    /// Define the return parameter from a procedure
    /// </summary>
    public class ADO_returnParam
    {
        #region Properties
        /// <summary>
        /// Return name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Return value
        /// </summary>
        public int value { get; set; }

        #endregion
    }

    /// <summary>
    /// Define the reader Metadata information
    /// </summary>
    public class ADO_readerMetadata
    {
        #region Properties
        /// <summary>
        /// Specific type
        /// </summary>
        public string specificType { get; internal set; }

        /// <summary>
        /// .NET type
        /// </summary>
        public string dotNetType { get; internal set; }

        /// <summary>
        /// SQL type
        /// </summary>
        public string sqlType { get; internal set; }

        #endregion
    }

    /// <summary>
    /// Define the reader Output, composed by the Meta information and a list of dynamic Data
    /// </summary>
    public class ADO_readerOutput
    {
        #region Properties
        /// <summary>
        /// Flag to indicate if any data exists
        /// </summary>
        public bool hasData { get; internal set; }

        /// <summary>
        /// Size of the output in Bytes
        /// </summary>
        public int sizeInByte { get; internal set; }

        /// <summary>
        /// Time spent
        /// </summary>
        public float timeInSec { get; internal set; }

        /// <summary>
        /// Meta output
        /// </summary>
        public List<dynamic> meta { get; internal set; }

        /// <summary>
        /// Data output
        /// </summary>
        public List<dynamic> data { get; internal set; }

        #endregion

        /// <summary>
        /// Initialise a blank Output 
        /// </summary>
        public ADO_readerOutput()
        {
            hasData = false;
            sizeInByte = 0;
            timeInSec = 0;
            meta = new List<dynamic>();
            data = new List<dynamic>();
        }
    }
}
