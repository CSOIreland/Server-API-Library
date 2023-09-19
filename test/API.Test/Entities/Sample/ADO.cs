using API;

namespace Sample
{
    [AllowAPICall]
    /// <summary>
    /// 
    /// </summary>
    public class YourADO
    {
        #region Methods

        /// <summary>
        /// This method is public and exposed to the API
        /// It always returns a JSONRPC_Output object
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        public static dynamic YourExposedMethod(JSONRPC_API apiRequest)
        {
            // Initiate new object to handle the API Output
            JSONRPC_Output output = new JSONRPC_Output();

            // Instantiate a new ADO, opening a SQL Server connection
            // The connection is up till closed or disposed
            ADO ado = new ADO("defaultConnection");

            try
            {
                // Start a Transation is needed
                // The Transation is active over multiple Queries
                ado.StartTransaction();

                // Add any input Parameters
                List<ADO_inputParams> inputParams = new List<ADO_inputParams>();

                // Execute the Reader
                ADO_readerOutput readerOutput = ado.ExecuteReaderProcedure(apiRequest.parameters.procedureName.ToString(), inputParams);

                bool isTransationFine = true;
                if (isTransationFine)
                {
                    // Complete a succesfull transaction
                    ado.CommitTransaction();
                }
                else
                {
                    // Rollback a failed transaction
                    ado.RollbackTransaction();
                }

                // Fetch the Reader's results
                if (readerOutput.hasData)
                {
                    // If all goes well, then return the data to the API, preferably as an object
                    output.data = readerOutput.data;

                    // Log a debug message to help yourself :-)
                    Log.Instance.Debug("Place your debug log-message here");
                }
                else
                {
                    // If an error/exception occurs, then return the error to the API
                    output.error = "place your error here, preferably as an object.";
                    // Log an error message for your sake :-)
                    Log.Instance.Error("Place your error log-message here");
                }

                // Remember to CloseConnection or Dispose the connection 
                ado.Dispose();
            }
            catch (Exception e)
            {
                // If an error/exception occurs, then return the error to the API
                output.error = e;
                // Log an error message for your sake :-)
                Log.Instance.Error(e);
            }

            return output;
        }

        /// <summary>
        /// This method is internal and not exposed to the API but accessible within your Assemply project.
        /// </summary>
        internal static void YourInternalMethod()
        {
            // All your internal business logic goes here
        }

        /// <summary>
        /// This method is private and not exposed to the API and accessible only within your Class.
        /// </summary>
        private static void YourPrivateMethods()
        {
            // All your private business logic goes here
        }

        #endregion
    }
}
