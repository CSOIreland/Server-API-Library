CREATE TABLE [dbo].[TM_APP_SETTING_CONFIG_VERSION] (
    [ASV_ID]      INT             IDENTITY (1, 1) NOT NULL,
    [ASV_VERSION] DECIMAL (10, 2) NOT NULL,
    [ASV_CST_ID] INT  NOT NULL, 
    PRIMARY KEY CLUSTERED ([ASV_ID] ASC), 
    CONSTRAINT [FK_TM_APP_SETTING_CONFIG_VERSION_TS_CONFIG_SETTING_TYPE] FOREIGN KEY (ASV_CST_ID) REFERENCES [TS_CONFIG_SETTING_TYPE]([CST_ID])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UQ_TM_APP_SETTING_CONFIG_VERSION]
    ON [dbo].[TM_APP_SETTING_CONFIG_VERSION]([ASV_VERSION] ASC, [ASV_CST_ID] ASC);

GO;

/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an insert on the TM_APP_SETTING_CONFIG_VERSION table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE OR ALTER TRIGGER TRIG_TM_APP_SETTING_CONFIG_VERSION_INSERT
    ON TM_APP_SETTING_CONFIG_VERSION
    AFTER INSERT
AS
BEGIN
        SET NOCOUNT ON

	 INSERT INTO TM_HISTORY_APP_SETTING_CONFIG_VERSION
                   (
                   [HSV_ASV_ID],
                     [HSV_VERSION], 
                     [HSV_CST_ID]
                    )
            SELECT A.ASV_ID, A.ASV_VERSION,A.ASV_CST_ID
            FROM TM_APP_SETTING_CONFIG_VERSION a
		    JOIN inserted i
			    ON a.ASV_ID = i.ASV_ID        

END

/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an update on the [TM_APP_SETTING_CONFIG_VERSION] table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE OR ALTER TRIGGER TRIG_TM_APP_SETTING_CONFIG_VERSION_UPDATE
    ON TM_APP_SETTING_CONFIG_VERSION
    AFTER UPDATE
AS
BEGIN
        SET NOCOUNT ON
		
            INSERT INTO TM_HISTORY_APP_SETTING_CONFIG_VERSION
                   (
                     [HSV_VERSION], 
                     [HSV_CST_ID],
                     [HSV_ASV_ID]
                    )
            SELECT A.ASV_VERSION,A.ASV_CST_ID,A.ASV_ID
            FROM TM_APP_SETTING_CONFIG_VERSION a
		    JOIN inserted i
			    ON a.ASV_ID = i.[ASV_ID]
END
GO

