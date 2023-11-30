CREATE TABLE [dbo].[TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY] (
    [HCD_ID]      INT             IDENTITY (1, 1) NOT NULL,
    [HCD_DEPLOYED_TIME] datetime NOT NULL  DEFAULT getdate(),  
    [HCD_HSV_ID] INT NOT NULL, 
	[HCD_IP_ADDRESS] varchar(100) NOT NULL, 
    PRIMARY KEY CLUSTERED ([HCD_ID] ASC), 
    CONSTRAINT [FK_TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY_TM_HISTORY_APP_SETTING_CONFIG_VERSION] FOREIGN KEY ([HCD_HSV_ID]) REFERENCES [TM_HISTORY_APP_SETTING_CONFIG_VERSION]([HSV_ID])
);

GO

 EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds records each time the application loads a configuration recordset' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY',
    @level2type = N'COLUMN',
    @level2name = N'HCD_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'when deployed',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY',
    @level2type = N'COLUMN',
    @level2name = N'HCD_DEPLOYED_TIME'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY',
    @level2type = N'COLUMN',
    @level2name = 'HCD_HSV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'ip address of server that loaded config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION_DEPLOY',
    @level2type = N'COLUMN',
    @level2name = N'HCD_IP_ADDRESS'