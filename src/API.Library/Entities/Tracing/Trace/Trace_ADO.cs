namespace API
{
    /// <summary>
    /// ADO classes for Trace
    /// </summary>
    internal static class Trace_ADO
    {
        /// <summary>
        /// Creates a Trace
        /// </summary>
        /// <param name="ado"></param>
        /// <param name="trace"></param>
        /// <param name="inTransaction"></param>
        /// <returns></returns>
        internal static void Create(Trace trace)
        {
            if (!ApiServicesHelper.ApiConfiguration.API_TRACE_ENABLED)
            {
                return;
            }

            ADO ado = string.IsNullOrEmpty(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE) ? new ADO() : new ADO(ApiServicesHelper.ADOSettings.API_TRACE_DATABASE);

            Log.Instance.Info("Trace information : " + Utility.JsonSerialize_IgnoreLoopingReference(trace));
            List<ADO_inputParams> inputParamList = new List<ADO_inputParams>()
            {
                new ADO_inputParams() {name= "@TrcUseragent",value=trace.TrcUseragent},
                new ADO_inputParams() {name= "@TrcStartTime",value=trace.TrcStartTime},
                new ADO_inputParams() {name= "@TrcDuration",value=trace.TrcDuration},
                new ADO_inputParams() {name= "@TrcStatusCode",value=trace.TrcStatusCode},
                new ADO_inputParams() {name= "@TrcMachineName",value=trace.TrcMachineName},
                new ADO_inputParams() {name= "@TrcRequestType", value = trace.TrcRequestType},
                new ADO_inputParams() {name= "@TrcRequestVerb", value = trace.TrcRequestVerb},
                new ADO_inputParams() {name= "@TrcCorrelationID", value = trace.TrcCorrelationID},
            };

            if (trace.TrcJsonRpcErrorCode != null)
                inputParamList.Add(new ADO_inputParams() { name = "@TrcJsonRpcErrorCode", value = trace.TrcJsonRpcErrorCode });

            if (!string.IsNullOrEmpty(trace.TrcErrorPath))
                inputParamList.Add(new ADO_inputParams() { name = "@TrcErrorPath", value = trace.TrcErrorPath });


            if (!string.IsNullOrEmpty(trace.TrcMethod))
                inputParamList.Add(new ADO_inputParams() { name = "@TrcMethod", value = trace.TrcMethod });

            if (!string.IsNullOrEmpty(trace.TrcParams))
                inputParamList.Add(new ADO_inputParams() { name = "@TrcParams", value = trace.TrcParams });

            if (ApiServicesHelper.ApiConfiguration.API_TRACE_RECORD_IP)
                inputParamList.Add(new ADO_inputParams() { name = "@TrcIp", value = trace.TrcIp });

            if (!string.IsNullOrEmpty(trace.TrcUsername))
                inputParamList.Add(new ADO_inputParams() { name = "@Username", value = trace.TrcUsername });

            // A return parameter is required for the operation
            ADO_returnParam retParam = new ADO_returnParam();
            retParam.name = "return";
            retParam.value = 0;
            try
            {
                ado.StartTransaction();
                //Executing the stored procedure
                ado.ExecuteNonQueryProcedure("Security_Trace_Create", inputParamList, ref retParam);

                ado.CommitTransaction();

                if (retParam.value != 1)
                {
                    Log.Instance.Fatal("Failed to store trace information");
                }

            }
            catch (Exception ex)
            {
                Log.Instance.Fatal("Error storing trace information : " + ex + ". Trace information : " + Utility.JsonSerialize_IgnoreLoopingReference(trace));
            }
            finally
            {
                ado.CloseConnection();
                ado.Dispose();
            }

            //return retParam.value;
        }
    }
}
