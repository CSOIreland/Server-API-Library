CREATE TABLE [dbo].[TS_CONFIG_SETTING_TYPE] (
    [CST_ID]      INT             IDENTITY (1, 1) NOT NULL,
    [CST_CODE] VARCHAR(10) NOT NULL,
    [CST_VALUE] VARCHAR(200) NOT NULL, 
    PRIMARY KEY CLUSTERED ([CST_ID] ASC)
);


GO;

CREATE UNIQUE NONCLUSTERED INDEX [IX_UQ_TS_CONFIG_SETTING_TYPE]
    ON [dbo].[TS_CONFIG_SETTING_TYPE]([CST_CODE]);


GO;

EXEC sp_addextendedproperty @name=N'MS_Description', 
@value=N'table that holds config settings types e.g. app and api' , @level0type=N'SCHEMA',@level0name=N'dbo', 
@level1type=N'TABLE',@level1name=N'TS_CONFIG_SETTING_TYPE'
GO

GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'primary key',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_CONFIG_SETTING_TYPE',
    @level2type = N'COLUMN',
    @level2name = N'CST_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'config type code',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_CONFIG_SETTING_TYPE',
    @level2type = N'COLUMN',
    @level2name = N'CST_CODE'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'config type value',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TS_CONFIG_SETTING_TYPE',
    @level2type = N'COLUMN',
    @level2name = N'CST_VALUE'