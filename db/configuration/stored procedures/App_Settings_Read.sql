/****** Object:  StoredProcedure [dbo].[App_Settings_Read]    Script Date: 02/05/2023 14:29:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/******************************************************************************** 
Author Name                           :  Stephen Lane 
Date written                          :  04/11/2022 
Version                               :  2 
Description   
              
Reads the application settings based on the version in the TM_APP_SETTING_VERSION table which is passed in. 
For c# application will come from appsettings.

EXEC App_Settings_Read 3.0,0,0

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 
1			23/03/2023	Stephen Lane			adding auto_version param so that latest version can always be retrieved instead of specific version
2			22/06/2023		Stephen Lane	dont return sensitive values unless requested
03			13/09/2023		Stephen Lane	changing mask to null to work with always encrypted

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE
	OR

ALTER PROCEDURE [dbo].[App_Settings_Read] @app_settings_version DECIMAL(10, 2)
	,@auto_version BIT
	,@read_sensitive_value BIT=1
AS
BEGIN
	SET NOCOUNT ON

	DECLARE @errorMessage VARCHAR(1300)
		,@version_id DECIMAL(10, 2) = NULL
		,@config_setting_type_id INT = NULL;

	SELECT @config_setting_type_id = cst_id
	FROM TS_CONFIG_SETTING_TYPE
	WHERE cst_code = 'APP'

	IF @config_setting_type_id IS NULL
	BEGIN
		SELECT @errorMessage = 'App setting type not found for code : APP';

		RAISERROR (
				@errorMessage
				,16
				,1
				)

		RETURN 0
	END

	SELECT @version_id = ASV_ID
	FROM TM_APP_SETTING_CONFIG_VERSION
	WHERE ASV_VERSION = @app_settings_version
		AND ASV_CST_ID = @config_setting_type_id;

	IF @version_id IS NULL
		AND @auto_version = 0
	BEGIN
		SELECT @errorMessage = CONCAT (
				'App setting version not found for version : '
				,@app_settings_version
				);

		RAISERROR (
				@errorMessage
				,16
				,1
				)

		RETURN 0
	END

	IF @auto_version = 1
	BEGIN
		DECLARE @max_version_id INT = NULL
			,@max_version_number DECIMAL(10, 2) = NULL;

		SELECT @max_version_id = max(ASV_ID)
			,@max_version_number = ASV_VERSION
		FROM TM_APP_SETTING_CONFIG_VERSION
		WHERE ASV_CST_ID = @config_setting_type_id
		GROUP BY ASV_ID
			,ASV_VERSION;

		IF @max_version_id IS NULL
		BEGIN
			SELECT @errorMessage = 'App setting version not found for latest version';

			RAISERROR (
					@errorMessage
					,16
					,1
					)

			RETURN 0
		END
		ELSE
		BEGIN

		IF @read_sensitive_value = 1
		BEGIN
			SELECT	APP_KEY
			,APP_VALUE
			,APP_DESCRIPTION 
			,APP_SENSITIVE_VALUE
			FROM TS_APP_SETTING
			WHERE APP_ASV_ID = @max_version_id;

			SELECT @max_version_number AS max_version_number;
		END
		ELSE
		BEGIN
			SELECT	APP_KEY
			,CASE WHEN APP_SENSITIVE_VALUE=0 THEN APP_VALUE ELSE null END AS APP_VALUE
			,APP_DESCRIPTION 
			,APP_SENSITIVE_VALUE
			FROM TS_APP_SETTING
			WHERE APP_ASV_ID = @max_version_id;

			SELECT @max_version_number AS max_version_number;
		END


		END
	END
	ELSE
	BEGIN
		IF @read_sensitive_value = 1
		BEGIN
			SELECT	APP_KEY
			,APP_VALUE
			,APP_DESCRIPTION 
			,APP_SENSITIVE_VALUE
			FROM TS_APP_SETTING
			WHERE APP_ASV_ID = @version_id;

			SELECT @app_settings_version AS max_version_number;
		END
		ELSE
		BEGIN
			SELECT	APP_KEY
			,CASE WHEN APP_SENSITIVE_VALUE=0 THEN APP_VALUE ELSE null END AS APP_VALUE
			,APP_DESCRIPTION 
			,APP_SENSITIVE_VALUE
			FROM TS_APP_SETTING
			WHERE APP_ASV_ID = @version_id;

			SELECT @app_settings_version AS max_version_number;
		END

	END
END
