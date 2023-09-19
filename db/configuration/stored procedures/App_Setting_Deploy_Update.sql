/******************************************************************************** 
Author Name                           :  Stephen Lane 
Date written                          :  27/01/2023
Version                               :  2 
Description   
              
Inserts a record for each time that a configuration is read from the database into c#

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 
1			23/03/2023	Stephen Lane			adding auto_version param so that latest version can always be retrieved instead of specific version

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE
	OR

ALTER PROCEDURE "dbo"."App_Setting_Deploy_Update" @app_settings_version DECIMAL(10, 2)
	,@config_setting_type VARCHAR(10), @auto_version bit = null
AS
BEGIN
	SET NOCOUNT ON

	DECLARE @errorMessage VARCHAR(1300)
		,@version_id DECIMAL(10, 2) = NULL
		,@config_setting_type_id INT = NULL;

	SELECT @config_setting_type_id = cst_id
	FROM TS_CONFIG_SETTING_TYPE
	WHERE cst_code = @config_setting_type;

	IF @config_setting_type_id IS NULL
	BEGIN
		SELECT @errorMessage = CONCAT (
				@config_setting_type
				,' setting type not found for code : ' + @config_setting_type
				);

		RAISERROR (
				@errorMessage
				,16
				,1
				)

		RETURN 0
	END

	if @auto_version = 1
		begin
			SELECT @version_id = max(ASV_ID)
			FROM TM_APP_SETTING_CONFIG_VERSION
			WHERE ASV_CST_ID = @config_setting_type_id;
		end
	else
		begin
			SELECT @version_id = ASV_ID
			FROM TM_APP_SETTING_CONFIG_VERSION
			WHERE ASV_VERSION = @app_settings_version
				AND ASV_CST_ID = @config_setting_type_id;
		end


	IF @version_id IS NULL
	BEGIN
		SELECT @errorMessage = CONCAT (
				@config_setting_type
				,' setting version not found for version : '
				,@app_settings_version
				);

		RAISERROR (
				@errorMessage
				,16
				,1
				)

		RETURN 0
	END

	DECLARE @machineIP AS VARCHAR(100);

	SELECT @machineIP = CONVERT(VARCHAR(100), CONNECTIONPROPERTY('client_net_address'))

	INSERT INTO [TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY] (
		[HCD_ASV_ID]
		,[HCD_IP_ADDRESS]
		)
	VALUES (
		@version_id
		,@machineIP
		)
END
