-- Create Expenses table in the cloud database
-- Run this against the cloud database (e.g., db33569.public.databaseasp.net)

-- Check if database exists (adjust name as needed)
USE [db33569];
GO

-- Create Expenses table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Expenses')
BEGIN
    CREATE TABLE [dbo].[Expenses] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Date] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Category] NVARCHAR(100) NOT NULL DEFAULT 'General',
        [Description] NVARCHAR(400) NULL,
        [Amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Reference] NVARCHAR(100) NULL
    );
    
    PRINT 'Expenses table created successfully.';
END
ELSE
BEGIN
    PRINT 'Expenses table already exists.';
END
GO

-- Verify table structure
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'Expenses' AND c.TABLE_SCHEMA = 'dbo'
ORDER BY c.ORDINAL_POSITION;
GO

-- Check row count
SELECT COUNT(*) AS ExpenseCount FROM [dbo].[Expenses];
GO
