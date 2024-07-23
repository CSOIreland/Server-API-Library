/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  24/10/2023
Version                               :  1 
Description   
              
Records Request information from the API

REVISION HISTORY 
---------------- 
REVIEW NO.    DATE          CHANGED BY         COMMENTS 
1			23/07/2024		Stephen Lane	added content length and referrer

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE or alter PROCEDURE Security_Trace_Create @TrcMethod NVARCHAR(256)  = null
	,@TrcParams NVARCHAR(2048)  = null
	,@TrcIp VARCHAR(15) = NULL
	,@TrcUseragent VARCHAR(2048)  = NULL
	,@Username NVARCHAR(256) = NULL
	,@TrcStartTime datetime
	,@TrcDuration decimal(18,3)
	,@TrcStatusCode int
	,@TrcMachineName varchar(256)
	,@TrcErrorPath varchar(1028) = null
	,@TrcRequestVerb varchar(50)
	,@TrcRequestType varchar(50) = null
	,@TrcCorrelationID varchar(1028)
	,@TrcJsonRpcErrorCode int  =null
	,@TrcContentLength bigint  =null
	,@TrcReferrer varchar(max)  =null
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO TD_API_TRACE (
		TRC_METHOD
		,TRC_PARAMS
		,TRC_IP
		,TRC_USERAGENT
		,TRC_USERNAME
		,TRC_DATETIME
		,TRC_STARTTIME
		,TRC_DURATION
		,TRC_STATUSCODE
		,TRC_MACHINENAME
		,TRC_REQUEST_TYPE
		,TRC_REQUEST_VERB
		,TRC_ERROR_PATH
		,TRC_CORRELATION_ID
		,TRC_JSONRPC_ERROR_CODE
		,TRC_CONTENT_LENGTH
		,TRC_REFERER)
	VALUES (
		 @TrcMethod
		,@TrcParams
		,@TrcIp
		,@Trcuseragent
		,@Username
		,getdate()
		,@TrcStartTime
		,@TrcDuration 
		,@TrcStatusCode
		,@TrcMachineName
		,@TrcRequestType
		,@TrcRequestVerb
		,@TrcErrorPath
		,@TrcCorrelationID
		,@TrcJsonRpcErrorCode
		,@TrcContentLength
		,@TrcReferrer
		)

	RETURN 1
END
GO


