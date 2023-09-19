CREATE TABLE [dbo].[TS_HISTORY_APP_SETTING] (
    [HPP_ID]          INT           IDENTITY (1, 1) NOT NULL,
    [HPP_ASV_ID]      INT           NOT NULL,
    [HPP_KEY]         VARCHAR (200) NOT NULL,
    [HPP_VALUE]       VARCHAR (MAX) NOT NULL,
    [HPP_DESCRIPTION] VARCHAR (MAX) NOT NULL,
    [HPP_APP_ID] INT NOT NULL, 
    [HPP_UPDATED_AT] DATETIME NOT NULL DEFAULT GETDATE(), 
    [HPP_UPDATED_BY] VARCHAR(256) NOT NULL DEFAULT CURRENT_USER, 
    [HPP_SENSITIVE_VALUE] BIT NOT NULL, 
    PRIMARY KEY CLUSTERED ([HPP_ID] ASC),
    CONSTRAINT [FK_TS_HISTORY_APP_SETTING_APP_SETTING_VERSION] FOREIGN KEY ([HPP_ASV_ID]) REFERENCES [dbo].[TM_APP_SETTING_CONFIG_VERSION] ([ASV_ID]),
    CONSTRAINT [FK_TS_HISTORY_APP_SETTING_APP_SETTING] FOREIGN KEY ([HPP_APP_ID]) REFERENCES [dbo].[TS_APP_SETTING] ([APP_ID])
);


GO

EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds historical app settings' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TS_HISTORY_APP_SETTING'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_ASV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'name of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_KEY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'value of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_VALUE'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'description of the app config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_DESCRIPTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'ts_app_setting foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_APP_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'when it was updated',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_UPDATED_AT'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'who updated it',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_UPDATED_BY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'flag to indicate if values are sensitive or not',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_APP_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPP_SENSITIVE_VALUE'