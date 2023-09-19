/****** Object:  StoredProcedure [dbo].[Api_Settings_Read]    Script Date: 02/05/2023 14:31:01 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/******************************************************************************** 
Author Name                           :  Stephen Lane 
Date written                          :  04/11/2022 
Version                               :  1 
Description   
              
Reads the application settings based on the version in the TM_APP_SETTING_VERSION table which is passed in. 
For c# application will come from appsettings.


REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 
01			22/06/2023		Stephen Lane	dont return sensitive values unless requested
02			13/09/2023		Stephen Lane	changing mask to null to work with always encrypted

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

exec api_settings_read 1

*************************************************************************************/
CREATE
	OR

ALTER PROCEDURE [dbo].[Api_Settings_Read] @app_settings_version DECIMAL(10, 2),
@read_sensitive_value BIT=1
AS
BEGIN
	SET NOCOUNT ON

	DECLARE @errorMessage VARCHAR(1300)
		,@version_id DECIMAL(10, 2) = NULL
		,@config_setting_type_id INT = NULL;

	SELECT @config_setting_type_id = cst_id
	FROM TS_CONFIG_SETTING_TYPE
	WHERE cst_code = 'API';

	IF @config_setting_type_id IS NULL
	BEGIN
		SELECT @errorMessage = 'Api setting type not found for code : API';

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
	BEGIN
		SELECT @errorMessage = CONCAT (
				'Api setting version not found for version : '
				,@app_settings_version
				);

		RAISERROR (
				@errorMessage
				,16
				,1
				)

		RETURN 0
	END

	IF @read_sensitive_value = 1
	BEGIN

		SELECT	API_KEY
		,API_VALUE
		,API_DESCRIPTION 
		,API_SENSITIVE_VALUE
		FROM TS_API_SETTING
		WHERE API_ASV_ID = @version_id;
	END
	ELSE
	BEGIN
		SELECT	API_KEY
		,CASE WHEN API_SENSITIVE_VALUE=0 THEN API_VALUE ELSE null END AS API_VALUE
		,API_DESCRIPTION 
		,API_SENSITIVE_VALUE
		FROM TS_API_SETTING
		WHERE API_ASV_ID = @version_id;

	END
END
