CREATE TABLE [dbo].[TS_APP_SETTING] (
    [APP_ID]          INT           IDENTITY (1, 1) NOT NULL,
    [APP_ASV_ID]      INT           NULL,
    [APP_KEY]         VARCHAR (200) NULL,
    [APP_VALUE]       VARCHAR (MAX) NULL,
    [APP_DESCRIPTION] VARCHAR (MAX) NULL,
	[APP_SENSITIVE_VALUE] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([APP_ID] ASC),
    CONSTRAINT [FK_TS_APP_SETTING_APP_SETTING_VERSION] FOREIGN KEY ([APP_ASV_ID]) REFERENCES [dbo].[TM_APP_SETTING_CONFIG_VERSION] ([ASV_ID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UQ_TS_APP_SETTING_APS_KEY]
    ON [dbo].[TS_APP_SETTING]([APP_KEY] ASC, [APP_ASV_ID] ASC);

 GO;

 EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds app settings' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TS_APP_SETTING'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'value of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_VALUE'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'name of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_KEY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'description of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_DESCRIPTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_ASV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'flag to indicate if values are sensitive or not',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'APP_SENSITIVE_VALUE'

/******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an insert on the TS_APP_SETTING table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE OR ALTER TRIGGER TRIG_TS_APP_SETTING_INSERT
    ON TS_APP_SETTING
    AFTER INSERT
AS
BEGIN
        SET NOCOUNT ON

	  INSERT INTO TS_HISTORY_APP_SETTING
                   (
                   [HPP_ASV_ID],
                   [HPP_KEY],        
                   [HPP_VALUE],      
                   [HPP_DESCRIPTION], 
                   [HPP_APP_ID],
                   [HPP_SENSITIVE_VALUE]
                   )
            SELECT a.APP_ASV_ID,a.APP_KEY,a.APP_VALUE,a.APP_DESCRIPTION,a.APP_ID,a.APP_SENSITIVE_VALUE
            FROM TS_APP_SETTING a
		    JOIN inserted i
			    ON a.APP_ID = i.APP_ID    

END

 /******************************************************************************** 
Author Name                           :  Stephen Lane
Date written                          :  27/01/23
Version                               :  1
Description   
              
Trigger for an update on the [TS_APP_SETTING] table.

REVISION HISTORY 
---------------- 
CHANGE NO.    DATE          CHANGED BY          REASON 

PEER REVIEW HISTORY 
------------------- 
REVIEW NO.    DATE          REVIEWED BY         COMMENTS 

*************************************************************************************/
CREATE OR ALTER TRIGGER TRIG_TS_APP_SETTING_UPDATE
    ON TS_APP_SETTING
    AFTER UPDATE
AS
BEGIN
        SET NOCOUNT ON
		
            INSERT INTO TS_HISTORY_APP_SETTING
                   (
                   [HPP_ASV_ID],
                   [HPP_KEY],        
                   [HPP_VALUE],      
                   [HPP_DESCRIPTION], 
                   [HPP_APP_ID],
                   [HPP_SENSITIVE_VALUE]
                   )
            SELECT a.APP_ASV_ID,a.APP_KEY,a.APP_VALUE,a.APP_DESCRIPTION,a.APP_ID,a.APP_SENSITIVE_VALUE
            FROM TS_APP_SETTING a
		    JOIN inserted i
			    ON a.APP_ID = i.APP_ID
END
GO