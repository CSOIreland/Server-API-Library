CREATE TABLE [dbo].[TS_HISTORY_API_SETTING] (
    [HPI_ID]          INT           IDENTITY (1, 1) NOT NULL,
    [HPI_ASV_ID]      INT           NOT NULL,
    [HPI_KEY]         VARCHAR (200) NOT NULL,
    [HPI_VALUE]       VARCHAR (MAX) NOT NULL,
    [HPI_DESCRIPTION] VARCHAR (MAX) NOT NULL,
    [HPI_UPDATED_AT] DATETIME NOT NULL DEFAULT GETDATE(), 
    [HPI_UPDATED_BY] VARCHAR(256) NOT NULL DEFAULT CURRENT_USER, 
    [HPI_API_ID] INT NOT NULL, 
    [HPI_SENSITIVE_VALUE] BIT NOT NULL, 
    PRIMARY KEY CLUSTERED ([HPI_ID] ASC),
    CONSTRAINT [FK_TS_HISTORY_API_SETTING_APP_SETTING_VERSION] FOREIGN KEY ([HPI_ASV_ID]) REFERENCES [dbo].[TM_APP_SETTING_CONFIG_VERSION] ([ASV_ID]),
    CONSTRAINT [FK_TS_HISTORY_API_SETTING_APP_SETTING] FOREIGN KEY ([HPI_API_ID]) REFERENCES [dbo].[TS_API_SETTING] ([API_ID])
);


GO

EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds historical api settings' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TS_HISTORY_API_SETTING'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'TM_APP_SETTING_CONFIG_VERSION foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_ASV_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_ID'
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'description of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_DESCRIPTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'name of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_KEY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'value of the api config item',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_VALUE'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'when it was updated',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_UPDATED_AT'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'who updated it',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_UPDATED_BY'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'ts_api_setting foreign key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_API_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'flag to indicate if values are sensitive or not',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_HISTORY_API_SETTING',
    @level2type = N'COLUMN',
    @level2name = N'HPI_SENSITIVE_VALUE'