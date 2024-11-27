CREATE TABLE [dbo].[TD_API_CACHE_TRACE](
	[CCH_ID] [int] IDENTITY(1,1) NOT NULL,
	[CCH_CORRELATION_ID] varchar(256) NULL,
	[CCH_OBJECT] VARCHAR(MAX) NULL,
	[CCH_START_TIME] datetime,
	[CCH_DURATION] decimal(10,3) NULL,
	[CCH_ACTION] varchar(2048) NULL,
	[CCH_SUCCESS] bit,
	[CCH_COMPRESSED_SIZE] int,	
	[CCH_EXPIRES_AT] datetime,
    [CCH_CACHE_LOCK_USED] BIT NULL, 
    [CCH_CACHE_LOCK_DURATION] DECIMAL(10, 3) NULL, 
    PRIMARY KEY CLUSTERED ([CCH_ID] ASC), 
)
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'api request correlation id',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_CORRELATION_ID'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'cache object details',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_OBJECT'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'start time of operation',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_START_TIME'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'duration of operation',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_DURATION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'action type',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_ACTION'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'successful or not',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_SUCCESS'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'object compressed size',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_COMPRESSED_SIZE'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'when cache item expires',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'TD_API_CACHE_TRACE',
    @level2type = N'COLUMN',
    @level2name = N'CCH_EXPIRES_AT'