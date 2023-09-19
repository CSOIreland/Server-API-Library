CREATE TABLE [dbo].[TS_API_SETTING] (
    [API_ID]          INT           IDENTITY (1, 1) NOT NULL,
    [API_ASV_ID]      INT           NOT NULL,
    [API_KEY]         VARCHAR (200) NOT NULL,
    [API_VALUE]       VARCHAR (MAX) NOT NULL,
    [API_DESCRIPTION] VARCHAR (MAX) NULL,
    [API_SENSITIVE_VALUE] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([API_ID] ASC),
    CONSTRAINT [FK_TS_API_SETTING_APP_SETTING_VERSION] FOREIGN KEY ([API_ASV_ID]) REFERENCES [dbo].[TM_APP_SETTING_CONFIG_VERSION] ([ASV_ID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UQ_TS_API_SETTING_APS_KEY]
    ON [dbo].[TS_API_SETTING]([API_KEY] ASC, [API_ASV_ID] ASC);

 GO;

 EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds api settings' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TS_API_SETTING'
GO

GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_ASV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'name of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_KEY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'value of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_VALUE'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'description of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_DESCRIPTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'flag to indicate if values are sensitive or not',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'API_SENSITIVE_VALUE'


	   /******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an update on the [TS_API_SETTING] table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 
01          19/06/2023      Stephen Lane        Added api_sensitive_value column


PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE TRIGGER TRIG_TS_API_SETTING_UPDATE
    ON TS_API_SETTING
    AFTER UPDATE
AS
BEGIN
        SET NOCOUNT ON
		
            INSERT INTO TS_HISTORY_API_SETTING
                   (
                   [HPI_ASV_ID],
                   [HPI_KEY],        
                   [HPI_VALUE],      
                   [HPI_DESCRIPTION], 
                   [HPI_API_ID],
                   [HPI_SENSITIVE_VALUE]
                   )
            SELECT a.API_ASV_ID,a.API_KEY,a.API_VALUE,a.API_DESCRIPTION,a.API_ID,a.API_SENSITIVE_VALUE
            FROM TS_API_SETTING a
		    JOIN inserted i
			    ON a.API_ID = i.API_ID
END
GO


/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an insert on the TS_API_SETTING table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 
01          19/06/2023      Stephen Lane        Added api_sensitive_value column

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE TRIGGER TRIG_TS_API_SETTING_INSERT
    ON TS_API_SETTING
    AFTER INSERT
AS
BEGIN
        SET NOCOUNT ON

	  INSERT INTO TS_HISTORY_API_SETTING
                   (
                   [HPI_ASV_ID],
                   [HPI_KEY],        
                   [HPI_VALUE],      
                   [HPI_DESCRIPTION], 
                   [HPI_API_ID],
                   [HPI_SENSITIVE_VALUE]
                   )
            SELECT a.API_ASV_ID,a.API_KEY,a.API_VALUE,a.API_DESCRIPTION,a.API_ID,a.API_SENSITIVE_VALUE
            FROM TS_API_SETTING a
		    JOIN inserted i
			    ON a.API_ID = i.API_ID    

END