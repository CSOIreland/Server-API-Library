CREATE TABLE [dbo].[TD_API_DATABASE_TRACE](
	[DBT_ID] [int] IDENTITY(1,1) NOT NULL,
	[DBT_CORRELATION_ID] varchar(256) NULL,
	[DBT_PROCEDURE_NAME] VARCHAR(1024) NULL,
	[DBT_PARAMS] VARCHAR(MAX) NULL,
	[DBT_START_TIME] datetime,
	[DBT_DURATION] decimal(10,3) NULL,
	[DBT_ACTION] varchar(2048) NULL,
	[DBT_SUCCESS] bit,
	PRIMARY KEY CLUSTERED ([DBT_ID] ASC)
)

GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'PRIMARY KEY',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'request correlation id',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_CORRELATION_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'name of the stored procedure',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_PROCEDURE_NAME'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'input parameters',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_PARAMS'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'start time of operation',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_START_TIME'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'duration',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_DURATION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'what type of event ',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_ACTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'action successful',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_DATABASE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'DBT_SUCCESS'