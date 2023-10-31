/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  24/10/2023
Version                               :  1 
Description   
              
Records Request information from the API

REVISION HISTORY 
---------------- 

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE PROCEDURE Trace_Create @TrcMethod NVARCHAR(256)  = null
	,@TrcParams NVARCHAR(2048)  = null
	,@TrcIp VARCHAR(15) = NULL
	,@TrcUseragent VARCHAR(2048)
	,@Username NVARCHAR(256) = NULL
	,@TrcStartTime datetime
	,@TrcDuration decimal(18,3)
	,@TrcStatusCode int
	,@TrcMachineName varchar(256)
	,@TrcErrorPath varchar(1028) = null
	,@TrcRequestVerb varchar(50)
	,@TrcRequestType varchar(50)
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
		)
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
		)

	RETURN 1
END
GO


