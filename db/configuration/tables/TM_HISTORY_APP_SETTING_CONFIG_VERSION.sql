CREATE TABLE [dbo].[TM_HISTORY_APP_SETTING_CONFIG_VERSION] (
    [HSV_ID]      INT             IDENTITY (1, 1) NOT NULL,
    [HSV_VERSION] DECIMAL (10, 2) NOT NULL,
    [HSV_CST_ID] INT  NOT NULL, 
    [HSV_UPDATED_AT] DATETIME NOT NULL DEFAULT GETDATE(), 
    [HSV_UPDATED_BY] VARCHAR(256) NOT NULL DEFAULT CURRENT_USER, 
    [HSV_ASV_ID] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([HSV_ID] ASC), 
    CONSTRAINT [FK_TM_HISTORY_APP_SETTING_CONFIG_VERSION_TS_CONFIG_SETTING_TYPE] FOREIGN KEY (HSV_CST_ID) REFERENCES [TS_CONFIG_SETTING_TYPE]([CST_ID]),
    CONSTRAINT [FK_TM_HISTORY_APP_SETTING_CONFIG_VERSION_TM_APP_SETTING_CONFIG_VERSION] FOREIGN KEY (HSV_ASV_ID) REFERENCES [TM_APP_SETTING_CONFIG_VERSION]([ASV_ID]),
);


GO

EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds the historical information of configuration version' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TM_HISTORY_APP_SETTING_CONFIG_VERSION'
GO



EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_VERSION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'ts_config_type foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_CST_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'when updated',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_UPDATED_AT'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'updated by',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_UPDATED_BY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TM_HISTORY_APP_SETTING_CONFIG_VERSION',
    @level2type = N'COLUMN',
    @level2name = N'HSV_ASV_ID'